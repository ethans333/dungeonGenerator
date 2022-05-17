 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GenerateDungeon : MonoBehaviour
{
    public Vector3 meanDimensions = new Vector3(10, 5, 10);
    public Vector3 minDimensions = new Vector3(3.5f, 1.5f, 3.5f);
    public int nRooms = 10;
    public float initRadius = 30f;
    public float hallwayWidth = 1.5f;

    Room[] rooms;

    bool moveShapes = true;

    void Start ()
    {
        generateRooms();
    }

    void Update ()
    {
        if (moveShapes)
        {
            int falseCount = 0;

            for (int i = 0; i < rooms.Length; i++)
            {
                if (!rooms[i].separationFlocking(rooms)) falseCount++;
            }

            if (falseCount == rooms.Length) {

                triangulateRooms();
                hallways();

                moveShapes = false;
            }
        }
    }

    Room[] generateRooms ()
    {
        rooms = new Room[nRooms];
        Vector3 dimensions, rPosition;
        GameObject roomsObj = new GameObject("Rooms");
        GameObject mainRoomsObj = new GameObject("Main Rooms");

        for (int i = 0; i < rooms.Length; i++)
        {
            Vector2 cPosition = Random.insideUnitCircle * initRadius;
            dimensions = new Vector3(boxMuller(meanDimensions.x, 2f), meanDimensions.y, boxMuller(meanDimensions.z, 2f));
            rPosition = new Vector3(cPosition.x, dimensions.y / 2, cPosition.y);
            rooms[i] = new Room(dimensions, rPosition, meanDimensions);

            if (rooms[i].mainRoom) {
                rooms[i].shape.transform.SetParent(mainRoomsObj.transform, true);
            } else {
                rooms[i].shape.transform.SetParent(roomsObj.transform, true);
            }
        }

        return rooms;
    }

    float boxMuller (float mu, float sigma)
    {
        float x = 0;
        float min = 0;

        for (int i = 0; i < 3; i++)
        {
            if (meanDimensions[i] == mu)
                min = minDimensions[i];
        }

        do {
            float u1 = Random.value;
            float u2 = Random.value;

            x = Mathf.Sqrt(-2 * Mathf.Log(u1)) * Mathf.Cos(2 * Mathf.PI * u2);

            x *= sigma;
            x += mu;

        } while (x <= min);


        return x;
    }

    class Room {

        public Vector3 dimensions, position;
        public GameObject shape;
        public BoxCollider collider;
        public bool mainRoom;
        public bool? isApart;

        public Room (Vector3 dimensions, Vector3 position, Vector3 meanDimensions)
        {
            this.dimensions = dimensions;
            this.position = position;
            this.shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            this.collider = this.shape.GetComponent(typeof(BoxCollider)) as BoxCollider;

            this.shape.transform.position = this.position;
            this.shape.transform.localScale = dimensions;

            this.mainRoom = (dimensions.x > meanDimensions.x * 1.05 && dimensions.z > meanDimensions.z * 1.05);
            this.shape.name = this.mainRoom ? "Main Room" : "Room";
        }

        public void update ()
        {
            this.shape.transform.position = this.position;
            this.shape.transform.localScale = this.dimensions;
        }

        public Room[] intersectingRooms (Room[] rooms)
        {
            Room[] intersectingRooms = new Room[0];

            for (int i = 0; i < rooms.Length; i++)
            {
                if (rooms[i] != this && rooms[i].collider.bounds.Intersects(this.collider.bounds))
                {
                    System.Array.Resize<Room>(ref intersectingRooms, intersectingRooms.Length + 1);
                    intersectingRooms.SetValue(rooms[i], intersectingRooms.Length - 1);
                }
            }

            return intersectingRooms;
        }

        public bool separationFlocking (Room[] agents)
        {
            bool movedShape = true;

            float vX = 0, vZ = 0;

            for (int i = 0; i < agents.Length; i++)
            {
                if (agents[i] != this && System.Array.Exists(this.intersectingRooms(agents), e => e == agents[i]))
                {
                    vX += agents[i].position.x - this.position.x;
                    vZ += agents[i].position.z - this.position.z;
                }
            }
            
            if (vX != 0 || vZ != 0)
            {
                vX /= agents.Length;
                vZ /= agents.Length;

                float vSum = Mathf.Sqrt(Mathf.Pow(vX, 2) + Mathf.Pow(vZ, 2));

                vX /= vSum;
                vZ /= vSum;

                vX *= -1;
                vZ *= -1;

                this.position.x += vX;
                this.position.z += vZ;
                this.update();

            } else if (vX == 0 && vZ == 0) {
                movedShape = false;
            }

            return movedShape;
        }

        public void increaseSize (Vector3 scale)
        {
            this.dimensions.x += this.dimensions.x * scale.x;
            this.dimensions.y += this.dimensions.y * scale.y;
            this.dimensions.z += this.dimensions.z * scale.z;
            this.update();
        }
    }

    Transform[] mainRoomsT ()
    {
        Transform[] mrTransforms = new Transform[0];

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].mainRoom)
            {
                System.Array.Resize<Transform>(ref mrTransforms, mrTransforms.Length + 1);
                mrTransforms.SetValue(rooms[i].shape.transform, mrTransforms.Length - 1);
            }
        }

        return mrTransforms;
    }

    Transform[] notApartT ()
    {
        Transform[] naTransforms = new Transform[0];

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].isApart == false)
            {
                System.Array.Resize<Transform>(ref naTransforms, naTransforms.Length + 1);
                naTransforms.SetValue(rooms[i].shape.transform, naTransforms.Length - 1);
            }
        }

        return naTransforms;
    }

    void triangulateRooms ()
    {
        Transform[] mrTransforms = mainRoomsT();

        for (int i = 0; i < mrTransforms.Length; i++)
        {
            for (int j = i; j < mrTransforms.Length; j++)
            {
                Vector3 mr = mrTransforms[i].position;
                Vector3 mrNeighbor = mrTransforms[j].position;

                Ray ray = new Ray(mr, mrNeighbor - mr);
                float distance = Vector3.Distance(mr, mrNeighbor);

                RaycastHit[] hits = Physics.RaycastAll(mr, mrNeighbor - mr, distance);

                for (int k = 0; k < rooms.Length; k++)
                {
                    bool isHit = System.Array.Exists(hits, h => h.transform == rooms[k].shape.transform);

                    if (isHit || rooms[k].mainRoom) rooms[k].isApart = false; else if (rooms[k].isApart == null) rooms[k].isApart = true;
                }

                Debug.DrawRay(mr, mrNeighbor - mr, Color.green, 99999f);
            }
        }

        for (int i = 0; i < rooms.Length; i++)
        {
            if (rooms[i].isApart == true) {
                //rooms[i].shape.transform.GetComponent<Renderer>().material.color = Color.red;
                Destroy(rooms[i].shape);
            } else if (rooms[i].mainRoom) {
                rooms[i].shape.transform.GetComponent<Renderer>().material.color = Color.magenta;
            } else {
                rooms[i].shape.transform.GetComponent<Renderer>().material.color = Color.blue;
            }
        }
    }

    void hallways ()
    {
        GameObject hallwaysObj = new GameObject("Hallways");
        Transform[] naTransforms = notApartT();

        for (int i = 0; i < naTransforms.Length; i++)
        {
            Vector3 p1 = naTransforms[i].position;
            Vector3 s1 = naTransforms[i].localScale;

            int[] occupiedSides = {0, 0, 0, 0};

            for (int j = 0; j < naTransforms.Length; j++)
            {

                Vector3 p2 = naTransforms[j].position;
                Vector3 s2 = naTransforms[j].localScale;

                if (p1.x - s1.x * 0.5f < p2.x + s2.x * 0.5f && p1.x + s1.x * 0.5f > p2.x - s2.x * 0.5f && p1 != p2) {
                    
                    float x = p2.x < p1.x ? (p2.x + s2.x*0.5f + p1.x - s1.x*0.5f)*0.5f : (p2.x - s2.x*0.5f + p1.x + s1.x*0.5f)*0.5f;
                    float zA = p2.z < p1.z ? p1.z - s1.z*0.5f : p1.z + s1.z*0.5f;
                    float zB = p2.z < p1.z ? p2.z + s2.z*0.5f : p2.z - s2.z*0.5f;

                    Vector3 pointA = new Vector3(x, p1.y, zA);
                    Vector3 pointB = new Vector3(x, p2.y, zB);

                    bool properWidth = Mathf.Abs(p2.x - p1.x) >= hallwayWidth;

                    if (properWidth && (p2.z > p1.z && occupiedSides[0] == 0 || p2.z < p1.z && occupiedSides[2] == 0)) drawHallway(pointA, pointB, hallwaysObj);
                    if (p2.z > p1.z) occupiedSides[0] = 1; else occupiedSides[2] = 1;
                }

                if (p1.z - s1.z * 0.5f < p2.z + s2.z * 0.5f && p1.z + s1.z * 0.5f > p2.z - s2.z * 0.5f && p1 != p2) {
                    
                    float z = p2.z < p1.z ? (p2.z + s2.z*0.5f + p1.z - s1.z*0.5f)*0.5f : (p2.z - s2.z*0.5f + p1.z + s1.z*0.5f)*0.5f;
                    float xA = p2.x < p1.x ? p1.x - s1.x*0.5f : p1.x + s1.x*0.5f;
                    float xB = p2.x < p1.x ? p2.x + s2.x*0.5f : p2.x - s2.x*0.5f;

                    Vector3 pointA = new Vector3(xA, p1.y, z);
                    Vector3 pointB = new Vector3(xB, p2.y, z);

                    bool properWidth = Mathf.Abs(p2.z - p1.z) >= hallwayWidth;
                    
                    if (properWidth && (p2.x > p1.x && occupiedSides[1] == 0 || p2.x < p1.x && occupiedSides[3] == 0)) drawHallway(pointA, pointB, hallwaysObj);
                    if (p2.x > p1.x) occupiedSides[1] = 1; else occupiedSides[3] = 1;
                }
            }
        }
    }

    void drawHallway (Vector3 pointA, Vector3 pointB, GameObject hallwaysObj)
    {
        Ray ray = new Ray(pointA, pointB - pointA);
        float d = Vector3.Distance(pointA, pointB);

        RaycastHit[] hits = Physics.RaycastAll(pointA, pointB - pointA, d);
        
        if (hits.Length == 1)
        {
            Debug.DrawRay(pointA, pointB - pointA, Color.blue, 99999f);
            Hallway h = new Hallway(pointA, pointB, hallwayWidth, meanDimensions.y - 0.1f);
            h.shape.transform.SetParent(hallwaysObj.transform, true);
        }
    }

    class Hallway
    {
        public GameObject shape;
        public Vector3 position;
        public BoxCollider collider;
        float length;

        public Hallway (Vector3 pointA, Vector3 pointB, float width, float height)
        {
            this.position = (pointA + pointB)*0.5f;
            this.length = pointA.x == pointB.x ? Mathf.Abs(pointA.z - pointB.z) : Mathf.Abs(pointA.x - pointB.x);
            this.shape = GameObject.CreatePrimitive(PrimitiveType.Cube);
            this.collider = this.shape.GetComponent(typeof(BoxCollider)) as BoxCollider;

            this.shape.name = "Hallway";
            this.shape.transform.position = this.position;
            this.shape.transform.localScale = pointA.x == pointB.x ? new Vector3(width, height, this.length) 
                : new Vector3(this.length, height, width);
        }
    }
}

