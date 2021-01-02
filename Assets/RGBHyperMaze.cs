using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class RGBHyperMaze : MonoBehaviour {

    new public KMAudio audio;
    public KMBombInfo info;

    public Transform balls;
    public Transform shafts;

    public KMSelectable display;
    
    public KMSelectable[] verticesSel;
    //public KMSelectable[] edgesSel;

    public MeshRenderer[] verticesCol;
    public MeshRenderer[] edgesCol;

    public TextMesh displayText;
    private string dispText;

    private bool revert;

    private bool[,,,] visited = new bool[2, 2, 2, 2];
    List<int[]> passable = new List<int[]>();
    List<int> passableTranslated = new List<int>();
    private System.Random rnd = new System.Random();
    private bool[,] rotationLogic = new bool[32, 5];
    private int[] rotations = new int[5];
    private int[] appliedRotations = new int[4];

    private bool start;
    private bool inputState;
    private bool visualState = true;

    private int[] XY = new int[32] {2,26,14,27,6,30,18,31,11,10,22,23,0,25,12,24,4,29,16,28,8,9,21,20,3,1,13,15,7,5,17,19};
    private int[] XZ = new int[32] {3,0,1,2,7,4,5,6,11,8,9,10,15,12,13,14,19,16,17,18,23,20,21,22,27,24,25,26,31,28,29,30};
    private int[] XW = new int[32] {2,10,6,11,0,9,4,8,3,1,5,7,14,22,18,23,12,21,16,20,15,13,17,19,27,26,30,31,24,25,29,28};
    private int[] YZ = new int[32] {24,3,27,15,28,7,31,19,20,8,11,23,25,1,26,13,29,5,30,17,21,9,10,22,12,0,2,14,16,4,6,18};
    private int[] YW = new int[32] {12,13,14,15,0,1,2,3,24,25,26,27,16,17,18,19,4,5,6,7,28,29,30,31,20,21,22,23,8,9,10,11};
    private int[] ZW = new int[32] {9,5,10,1,8,7,11,3,0,4,6,2,21,17,22,13,20,19,23,15,12,16,18,14,25,29,30,26,24,28,31,27};

    private int[] operatorID = new int[32] {2,0,2,0,2,0,2,0,3,3,3,3,2,0,2,0,2,0,2,0,3,3,3,3,1,1,1,1,1,1,1,1};

    private Vector4[] fDCoords = new Vector4[16] {
        new Vector4(-1f,-1f,-1f,-1f),
        new Vector4(-1f,-1f,1f,-1f),
        new Vector4(1f,-1f,1f,-1f),
        new Vector4(1f,-1f,-1f,-1f),
        new Vector4(-1f,-1f,-1f,1f),
        new Vector4(-1f,-1f,1f,1f),
        new Vector4(1f,-1f,1f,1f),
        new Vector4(1f,-1f,-1f,1f),
        new Vector4(-1f,1f,-1f,-1f),
        new Vector4(-1f,1f,1f,-1f),
        new Vector4(1f,1f,1f,-1f),
        new Vector4(1f,1f,-1f,-1f),
        new Vector4(-1f,1f,-1f,1f),
        new Vector4(-1f,1f,1f,1f),
        new Vector4(1f,1f,1f,1f),
        new Vector4(1f,1f,-1f,1f),
    };

    private Vector4[] flipCoord = new Vector4[4] {
        new Vector4(1f,-1f,-1f,-1f),
        new Vector4(-1f,1f,-1f,-1f),
        new Vector4(-1f,-1f,1f,-1f),
        new Vector4(-1f,-1f,-1f,1f),
    };

    private int startingVertex;
    private int goalVertex;
    private Vector4 startingCoords;
    private Vector4 goalCoords;
    private int currentVertex;

    private int[] edgeLeft  = new int[32] {0,1,2,3,4,5,6,7,0,1,2,3,8,9,10,11,12,13,14,15,8,9,10,11,0,1,2,3,4,5,6,7};
    private int[] edgeRight = new int[32] {1,2,3,0,5,6,7,4,4,5,6,7,9,10,11,8,13,14,15,12,12,13,14,15,8,9,10,11,12,13,14,15};

    private Transform[] verticesTf = new Transform[16];
    private Transform[] edgesTf = new Transform[32];

    private int step = 0;
    private int transitionProg = 0;
    private int revertProg = 0;
    private int solveProg = 0;

    Color[] vColInit = new Color[16];
    Color[] eColInit = new Color[32];

    Color[] verticesColPuzzle = new Color[16];
    Color[] edgesColPuzzle = new Color[32];

    Color spinVertexColor;
    Color spinEdgeColor;

    private Vector3[,] verticesInitTransform = new Vector3[16,2];
    private Vector3[,] edgesInitTransform = new Vector3[32,2];

    static int moduleIdCounter = 1;
    int moduleId;
    private bool moduleSolved;

    private static readonly string[][] _dimensionNames = new[] { new[] { "left", "right" }, new[] { "bottom", "top" }, new[] { "front", "back" }, new[] { "zig", "zag" } };
    private static readonly int[] timwisBetterOrderTranslator = new int[] {0,3,8,11,1,2,9,10,4,7,12,15,5,6,13,14};

    private void Awake()
    {
        moduleId = moduleIdCounter++;

        startingVertex = rnd.Next(16); //choose random vertex to start and set it to current location; then choose a goal that is 3 moves away
        currentVertex = startingVertex;
        startingCoords = fDCoords[startingVertex];
        goalCoords = Vector4.Scale(startingCoords,flipCoord[rnd.Next(4)]);
        for(int i = 0; i < 16; i++) { if(fDCoords[i] == goalCoords) { goalVertex = i; } }

        display.OnInteract += delegate () { PressDisplay(); return false; };
        for(int i = 0; i < 16; i++) { verticesSel[i].OnInteract = PressVertex(i); }

        for (int ID = 0; ID < balls.childCount; ID++)  //Get Transforms as an array from all vertices and edges
        {
            verticesTf[ID] = balls.GetChild(ID).GetComponent<Transform>();
            verticesInitTransform[ID,0] = verticesTf[ID].localPosition;
            verticesInitTransform[ID,1] = verticesTf[ID].localEulerAngles;
        }
        for (int ID = 0; ID < shafts.childCount; ID++)
        {
            edgesTf[ID] = shafts.GetChild(ID).GetComponent<Transform>();
            edgesInitTransform[ID,0] = edgesTf[ID].localPosition;
            edgesInitTransform[ID,1] = edgesTf[ID].localEulerAngles;
        }

        float hue = rnd.Next(100);                              //choose a random start color for stage 1 and set all vertices and edges to it
        spinEdgeColor = Color.HSVToRGB(hue/100,1f,0.7f,true);
        spinVertexColor = Color.HSVToRGB(hue/100,1f,0.5f,true);

        for(int i = 0; i < 32; i++) { edgesCol[i].material.color = spinEdgeColor; }
        for(int i = 0; i < 16; i++) { verticesCol[i].material.color = spinVertexColor; }
        
        List<int> possibleRots = new List<int>();               //choose rotations without duplicates
        for(int i = 0; i < 12; i++) { possibleRots.Add(i); }
        for(int i = 0; i < 5; i++)
        {
            int nextRot = rnd.Next(possibleRots.Count);
            rotations[i] = possibleRots[nextRot];
            possibleRots.RemoveAt(nextRot);
        }

        BackTracker(0, 0, 0, 0); //generate maze

        for (int i = 0; i < passable.Count; i++) { passableTranslated.Add(edgeTranslator(passable[i])); } //translate maze output into an array 
        while(passableTranslated.Count < 20)
        {
            int nextEdge = rnd.Next(32);
            bool add = true;
            for (int i = 0; i < passableTranslated.Count; i++)
            {
                if(passableTranslated[i] == nextEdge)
                {
                    add = false;
                }
            }
            if (add)
            {
                passableTranslated.Add(nextEdge);
            }
        }
        for(int i = 0; i < 32; i++) { rotationLogic[i,0] = true; }
        for (int i = 0; i < passableTranslated.Count; i++) { rotationLogic[passableTranslated[i],0] = false; }
        
        List<int> possibleAppRots = new List<int>();                //choose a random order to apply the rotations, with no duplicates
        for(int i = 0; i < 5; i++) { possibleAppRots.Add(i); }
        for(int i = 0; i < 4; i++){
            int nextRot = rnd.Next(possibleAppRots.Count);
            appliedRotations[i] = possibleAppRots[nextRot];
            possibleAppRots.RemoveAt(nextRot);
        }
        for(int i = 1; i < 5; i++)                              //applies the rotation order on the array to get final maze
        {
            performRotation(rotations[appliedRotations[0]], 0);
            performRotation(rotations[appliedRotations[1]], 1);
            performRotation(rotations[appliedRotations[2]], 2);
            performRotation(rotations[appliedRotations[3]], 3);
        }
        for(int i = 0; i < 4; i++) { appliedRotations[i] += 1; }                                            //convert the order into a string for display
        dispText = string.Join("", new List<int>(appliedRotations).ConvertAll(i => i.ToString()).ToArray());

        ColorPuzzler();                                 //generate colors that translate to the maze
        
        //logging shit
        
        logRots(rotations, appliedRotations);
        logMazes(verticesColPuzzle,edgesColPuzzle);
        
    }

    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
        if(moduleSolved && solveProg < 300){ //runs the solve anim
            solveAnim(solveProg);
            solveProg += 1;
        }
        else if(moduleSolved){} //makes sure Shit all happens after the mod solves
        else if(revert) //handles return to stage 1 on strike
        {
            visualState = true;
            inputState = false;
            start = false;
            revertProg += 1;
            for(int i = 0; i < 32; i++)
            {
                if(i < 16)
                {
                    verticesCol[i].material.color = Color.Lerp(Color.black,spinVertexColor,(float) revertProg/100);
                }
                edgesCol[i].material.color = Color.Lerp(Color.black,spinEdgeColor,(float) revertProg/100);
            }
            if(revertProg == 100)
            {
                revert = false;
                revertProg = 0;
            }
        }
        else if(!start || step % 200 >= 100 || step % 200 < 50){ //handles rotations on stage 1
            displayText.text = "----";
            step += 1;

            if(step > 100 && step <= 250)
            {
                rotate(step-100, rotations[0]);
            }
            else if(step > 300 && step <= 450)
            {
                rotate(step-300, rotations[1]);
            }
            else if(step > 500 && step <= 650)
            {
                rotate(step-500, rotations[2]);
            }
            else if(step > 700 && step <= 850)
            {
                rotate(step-700, rotations[3]);
            }
            else if(step > 900 && step <= 1050)
            {
                rotate(step-900, rotations[4]);
            }
            if(step == 1100)
            {
                step = 0;
            }
        }
        else
        {
            if(inputState == true && visualState == false){ //handles transition from stage 2 to stage 3
                if(transitionProg == 0){
                    for(int i = 0; i < 32; i++)
                    {
                        if(i < 16)
                        {
                            vColInit[i] = verticesCol[i].material.color;
                        }
                        eColInit[i] = edgesCol[i].material.color;
                    }
                }
                for(int i = 0; i < 32; i++)
                {
                    if(i < 16)
                    {
                        verticesCol[i].material.color = Color.Lerp(vColInit[i],Color.HSVToRGB(0f,0f,0.8f),(float) transitionProg/100);
                    }
                    edgesCol[i].material.color = Color.Lerp(eColInit[i],Color.HSVToRGB(0f,0f,0.6f),(float) transitionProg/100);
                }
                transitionProg += 1;
                if(transitionProg == 101)
                {
                    transitionProg = 0;
                    visualState = inputState;
                }
            }
            if(inputState == false && visualState == true){ //handles transition from stage 3 to stage 2
                if(transitionProg == 0){
                    for(int i = 0; i < 32; i++)
                    {
                        if(i < 16)
                        {
                            vColInit[i] = verticesCol[i].material.color;
                        }
                        eColInit[i] = edgesCol[i].material.color;
                    }
                }
                for(int i = 0; i < 32; i++)
                {
                    if(i < 16)
                    {
                        verticesCol[i].material.color = Color.Lerp(vColInit[i],verticesColPuzzle[i],(float) transitionProg/100);
                    }
                    edgesCol[i].material.color = Color.Lerp(eColInit[i],edgesColPuzzle[i],(float) transitionProg/100);
                }
                transitionProg += 1;
                if(transitionProg == 101)
                {
                    transitionProg = 0;
                    visualState = inputState;
                }
            }
            if(inputState == true && visualState == true) //handles stage 3, colors goal and current vertices
            {
                for(int i = 0; i < 16; i++)
                {
                    if(i == goalVertex)
                    {
                        verticesCol[i].material.color = new Color(0f,0.8f,0f);
                    }
                    else if(i == currentVertex)
                    {
                        verticesCol[i].material.color = new Color(0.8f,0f,0f);
                    }
                    else
                    {
                        verticesCol[i].material.color = Color.HSVToRGB(0f,0f,0.8f);
                    }
                }
                /*for(int i = 0; i < 32; i++){ //DEBUG
                    if(rotationLogic[i,4]){
                        edgesCol[i].material.color = new Color(1f,0f,0f);
                    }
                    else
                    {
                        edgesCol[i].material.color = new Color(0f,1f,0f);
                    }  
                }*/
            }
            else if(inputState == false && visualState == false && !moduleSolved) // makes the text pop up on stage 2
            {
                displayText.text = dispText;
            }
        }
	}

    void PressDisplay() //toggles/progresses stages
    {
        display.AddInteractionPunch(1f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.BigButtonPress, transform);

        if (!start)
        {
            start = true;
        }
        else if(inputState == visualState)
        {
            inputState = !inputState;
        }
    }
    private KMSelectable.OnInteractHandler PressVertex(int id) //basically does nothing unless stage 3
    {
        return delegate{
            string vertexName = "";
            string edgeName = "";
            verticesSel[id].AddInteractionPunch(0.2f);
            audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
            if(visualState && inputState)                   //most of this stuff is just logging but more importantly it moves the current vertex if applicable, and strikes if applicable
            {
                if(fDCoords[id].x == -1) { vertexName += "left-"; } else { vertexName += "right-"; }
                if(fDCoords[id].y == -1) { vertexName += "bottom-"; } else { vertexName += "top-"; }
                if(fDCoords[id].z == -1) { vertexName += "front-"; } else { vertexName += "back-"; }
                if(fDCoords[id].w == -1) { vertexName += "zig"; } else { vertexName += "zag"; }

                int passingEdge = -1;
                for(int i = 0; i < 32; i++){
                    if((id == edgeLeft[i] && currentVertex == edgeRight[i]) || (id == edgeRight[i] && currentVertex == edgeLeft[i]))
                    {
                        passingEdge = i;
                        if(fDCoords[edgeLeft[i]].x == -1 && fDCoords[edgeRight[i]].x == -1) { edgeName += "left-"; } else if(fDCoords[edgeLeft[i]].x == 1 && fDCoords[edgeRight[i]].x == 1) { edgeName += "right-"; }
                        if(fDCoords[edgeLeft[i]].y == -1 && fDCoords[edgeRight[i]].y == -1) { edgeName += "bottom-"; } else if(fDCoords[edgeLeft[i]].y == 1 && fDCoords[edgeRight[i]].y == 1) { edgeName += "top-"; }
                        if(fDCoords[edgeLeft[i]].z == -1 && fDCoords[edgeRight[i]].z == -1) { edgeName += "front"; } else if(fDCoords[edgeLeft[i]].z == 1 && fDCoords[edgeRight[i]].z == 1) { edgeName += "back"; }
                        if(fDCoords[edgeLeft[i]].w == fDCoords[edgeRight[i]].w && fDCoords[edgeLeft[i]].z == fDCoords[edgeRight[i]].z) { edgeName += "-";}
                        if(fDCoords[edgeLeft[i]].w == -1 && fDCoords[edgeRight[i]].w == -1) { edgeName += "zig"; } else if(fDCoords[edgeLeft[i]].w == 1 && fDCoords[edgeRight[i]].w == 1) { edgeName += "zag"; }
                    }
                }
                if(passingEdge == -1)
                {
                    return false;
                }
                else
                {
                    if(rotationLogic[passingEdge,4] && !moduleSolved)
                    {
                        
                        Debug.LogFormat("[RGB Hypermaze #{0}] Attempted to move to " + vertexName + ", but " + edgeName + " was a wall. Strike!", moduleId);
                        GetComponent<KMBombModule>().HandleStrike();
                        revert = true;
                    }
                    else
                    {
                        currentVertex = id;
                        Debug.LogFormat("[RGB Hypermaze #{0}] Successfully moved to " + vertexName + ".", moduleId);
                        if(currentVertex == goalVertex)
                        {
                            Debug.LogFormat("[RGB Hypermaze #{0}] Goal vertex reached. Module solved!", moduleId);
                            moduleSolved = true;
                            //audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
                        }
                    }
                }
            }
            return false;   
        };
    }

    public void rotate(int progress, int rotId) //performs the rotations on stage 1
    {
        if(progress == 150)
        {
            progress = 0;
        }
        double smoothProg = Math.Sin(Math.PI*progress/290);
        smoothProg *= smoothProg * 100;
        
        for(int i = 0; i < 16; i ++)
        {

            double rad = Math.Sqrt(2);
            double deg = 0;
            double newX = fDCoords[i].x;
            double newY = fDCoords[i].y;
            double newZ = fDCoords[i].z;
            double newW = fDCoords[i].w;
            if(rotId>=6){
                deg -= (double)smoothProg*9/10*Math.PI/180;
            }else{
                deg += (double)smoothProg*9/10*Math.PI/180;
            }
            switch (rotId%6)
            {
                case 0:
                    deg += Math.Atan(fDCoords[i].y/fDCoords[i].x);
                    if(fDCoords[i].x < 0)
                    {
                        deg += Math.PI;
                    }
                    newX = rad*Math.Cos(deg);
                    newY = rad*Math.Sin(deg);
                    break;
                case 1:
                    deg += Math.Atan(fDCoords[i].z/fDCoords[i].x);
                    if(fDCoords[i].x < 0)
                    {
                        deg += Math.PI;
                    }
                    newX = rad*Math.Cos(deg);
                    newZ = rad*Math.Sin(deg);
                    break;
                case 2:
                    deg += Math.Atan(fDCoords[i].w/fDCoords[i].x);
                    if(fDCoords[i].x < 0)
                    {
                        deg += Math.PI;
                    }
                    newX = rad*Math.Cos(deg);
                    newW = rad*Math.Sin(deg);
                    break;
                case 3:
                    deg += Math.Atan(fDCoords[i].z/fDCoords[i].y);
                    if(fDCoords[i].y < 0)
                    {
                        deg += Math.PI;
                    }
                    newY = rad*Math.Cos(deg);
                    newZ = rad*Math.Sin(deg);
                    break;
                case 4:
                    deg += Math.Atan(fDCoords[i].w/fDCoords[i].y);
                    if(fDCoords[i].y < 0)
                    {
                        deg += Math.PI;
                    }
                    newY = rad*Math.Cos(deg);
                    newW = rad*Math.Sin(deg);
                    break;
                case 5:
                    deg += Math.Atan(fDCoords[i].w/fDCoords[i].z);
                    if(fDCoords[i].z < 0)
                    {
                        deg += Math.PI;
                    }
                    newZ = rad*Math.Cos(deg);
                    newW = rad*Math.Sin(deg);
                    break;
                default:
                    return;

            }
            
            verticesTf[i].localPosition = convert4DTo3D(newX,newY,newZ,newW);

        }
        for(int i = 0; i < 32; i ++)
        {
            edgesTf[i].localPosition = Vector3.Lerp(verticesTf[edgeLeft[i]].localPosition,verticesTf[edgeRight[i]].localPosition,0.5f);
            float d_x = verticesTf[edgeLeft[i]].localPosition.x-verticesTf[edgeRight[i]].localPosition.x;
            float d_y = verticesTf[edgeLeft[i]].localPosition.y-verticesTf[edgeRight[i]].localPosition.y;
            //float d_z = verticesTf[edgeLeft[i]].localPosition.z-verticesTf[edgeRight[i]].localPosition.z;
            float d_deg = (float) (Math.Atan(d_y/d_x)*180/Math.PI);
            d_deg *= -1;
            //float newDeg = (float) edgesInitTransform[i,1].x+d_deg;
            //edgesTf[i].localEulerAngles = new Vector3(0,0,90-d_deg);
            edgesTf[i].localRotation = Quaternion.FromToRotation(Vector3.up, verticesTf[edgeLeft[i]].localPosition - verticesTf[edgeRight[i]].localPosition);
            float length = (verticesTf[edgeLeft[i]].localPosition-verticesTf[edgeRight[i]].localPosition).magnitude;

            edgesTf[i].localScale = new Vector3(edgesTf[i].localScale.x, length/2, edgesTf[i].localScale.z);
        }
    }

    public Vector3 convert4DTo3D(double x, double y, double z, double w)    //All vertex positions are rotated in a 4d coord system, 
    {                                                                       //then translated back to 3d (with a system i chose arbitrarily based on timwi's)
        double newX = 0.035*x+0.015*w;
        double newY = 0.08+0.035*y+0.015*w;
        double newZ = 0.035*z+0.015*w;
        Vector3 result = new Vector3((float) newX,(float) newY,(float) newZ);
        return result;
    }

    public void BackTracker(int x, int y, int z, int w)             //mazegen code. follows the following algorithm:
    {                                                               //1. Choose a cell you haven't visited.
        visited[x, y, z, w] = true;                                 //2. Add the path between current cell and new cell to a list of "passable" paths
        List<int[]> neighbors = new List<int[]>();                  //3. Set "current cell" to the new cell, and start from step 1 with reference to the new cell.
        neighbors.Add(new int[] { 0 });                             //If a cell has no more adjacent cells that have not been visited, return to the cell that referenced it.
                                                                    //Algorithm completes when no cells are unvisited
        while(neighbors.Count > 0)                                  //More info: https://en.wikipedia.org/wiki/Maze_generation_algorithm
        {                                                           //Scroll to "Recursive implementation" of "randomized depth-first search".
            neighbors.Clear();                                      //Also referred to as "Recursive Backtracker" maze generation.
            if (!visited[(x + 1) % 2, y, z, w])
            {
                neighbors.Add(new int[] { (x + 1) % 2, y, z, w });
            }
            if (!visited[x, (y + 1) % 2, z, w])
            {
                neighbors.Add(new int[] { x, (y + 1) % 2, z, w });
            }
            if (!visited[x, y, (z + 1) % 2, w])
            {
                neighbors.Add(new int[] { x, y, (z + 1) % 2, w });
            }
            if (!visited[x, y, z, (w + 1) % 2])
            {
                neighbors.Add(new int[] { x, y, z, (w + 1) % 2 });
            }
            if(neighbors.Count > 0)
            {
                int id = rnd.Next(neighbors.Count());
                int next_x = neighbors[id][0];
                int next_y = neighbors[id][1];
                int next_z = neighbors[id][2];
                int next_w = neighbors[id][3];
                int edge_x = ((2 * x - 1) + (2 * next_x - 1))/2;
                int edge_y = ((2 * y - 1) + (2 * next_y - 1))/2;
                int edge_z = ((2 * z - 1) + (2 * next_z - 1))/2;
                int edge_w = ((2 * w - 1) + (2 * next_w - 1))/2;
                passable.Add(new int[] { edge_x, edge_y, edge_z, edge_w});
                BackTracker(next_x, next_y, next_z, next_w);
            }

        }
        return;
    }

    public void ColorPuzzler()
    {
        /*int vertexParity = rnd.Next(2);                  //Shit Procedural, Carefully thought out code
        int primaryCol = rnd.Next(3);
        int secondaryCol = rnd.Next(2);
        Color primary = new Color(0f,0f,0f);
        Color secondary = new Color(0f,0f,0f);
        switch(primaryCol)                                
        {
            case 0:
                primary = new Color(1f, 0f, 0f);
                if(secondaryCol == 0)
                {
                    secondary = new Color(1f, 1f, 0f);
                }
                else
                {
                    secondary = new Color(1f, 0f, 1f);
                }
                break;
            case 1:
                primary = new Color(0f, 1f, 0f);
                if(secondaryCol == 0)
                {
                    secondary = new Color(1f, 1f, 0f);
                }
                else
                {
                    secondary = new Color(0f, 1f, 1f);
                }
                break;
            case 2:
                primary = new Color(0f, 0f, 1f);
                if(secondaryCol == 0)
                {
                    secondary = new Color(1f, 0f, 1f);
                }
                else
                {
                    secondary = new Color(0f, 1f, 1f);
                }
                break;
            default:
                break;
        }
        for(int i = 0; i < 16; i ++)
        {
            int parity = (int) (fDCoords[i].x*fDCoords[i].y*fDCoords[i].z*fDCoords[i].w);
            if(parity == -1){
                parity = 0;
            }
            if(parity == vertexParity)
            {
                verticesColPuzzle[i] = secondary;
            }
            else
            {
                verticesColPuzzle[i] = primary;
            }
        }
        for(int i = 0; i < 32; i++)
        {
            switch(operatorID[i])
            {
                case 0:
                    if(rotationLogic[i,0] == true)
                    {
                        
                    }
            }
        }
        return;*/

        //            EPIC RANDOM GENERATED TRIAL AND ERROR CODE
        bool reset = true;
        int failureCount = 0;   //Exactly as i just said. Chooses random vertex colors and guesses random edge colors to see if they work. There is always a solution to generate. 
        while(reset)            //DM me on discord if you want proof, i'd love to discuss: @__#1507. This code should not fail to generate a maze,
        {                       //but please let me know if it gets stuck in the while loop. I tested for quite a while to make sure it didn't happen.
            reset = false;
            failureCount += 1;
            int[,] vertRNGPuzzle = new int[16,3];
            int[,] edgeRNGPuzzle = new int[32,3];
            for(int i = 0; i < 16; i++)
            {
                vertRNGPuzzle[i,0] = rnd.Next(2);
                vertRNGPuzzle[i,1] = rnd.Next(2);
                vertRNGPuzzle[i,2] = rnd.Next(2);
            }
            for(int i = 0; i < 32; i++)
            {
                List<int> possibleCol = new List<int>();
                switch(operatorID[i]) 
                {
                    case 0:
                        for(int j = 0; j < 3; j++)
                        {
                            if((vertRNGPuzzle[edgeLeft[i],j] + vertRNGPuzzle[edgeRight[i],j] == 2) == (rotationLogic[i,0]))
                            {
                                possibleCol.Add(j);
                            }
                        }
                        if(possibleCol.Count == 0)
                        {
                            reset = true;
                        }
                        else
                        {
                            int rndColInt = possibleCol[rnd.Next(possibleCol.Count)];
                            edgeRNGPuzzle[i,rndColInt] = 1;
                        }
                        break;
                    case 1:
                        for(int j = 0; j < 3; j++)
                        {
                            if((vertRNGPuzzle[edgeLeft[i],j] + vertRNGPuzzle[edgeRight[i],j] >= 1) == (rotationLogic[i,0]))
                            {
                                possibleCol.Add(j);
                            }
                        }
                        if(possibleCol.Count == 0)
                        {
                            reset = true;
                        }
                        else
                        {
                            int rndColInt = possibleCol[rnd.Next(possibleCol.Count)];
                            edgeRNGPuzzle[i,rndColInt] = 1;
                        }
                        break;
                    case 2:
                        for(int j = 0; j < 3; j++)
                        {
                            if((vertRNGPuzzle[edgeLeft[i],j] + vertRNGPuzzle[edgeRight[i],j] != 2) == (rotationLogic[i,0]))
                            {
                                possibleCol.Add(j);
                            }
                        }
                        if(possibleCol.Count == 0)
                        {
                            reset = true;
                        }
                        else
                        {
                            int rndColInt = possibleCol[rnd.Next(possibleCol.Count)];
                            edgeRNGPuzzle[i,rndColInt] = 1;
                        }
                        break;
                    case 3:
                        for(int j = 0; j < 3; j++)
                        {
                            if((vertRNGPuzzle[edgeLeft[i],j] + vertRNGPuzzle[edgeRight[i],j] == 0) == (rotationLogic[i,0]))
                            {
                                possibleCol.Add(j);
                            }
                        }
                        if(possibleCol.Count == 0)
                        {
                            reset = true;
                        }
                        else
                        {
                            int rndColInt = possibleCol[rnd.Next(possibleCol.Count)];
                            edgeRNGPuzzle[i,rndColInt] = 1;
                        }
                        break;
                    
                }
                if(reset)
                {
                    edgeRNGPuzzle = new int[32,3];
                    break;
                }
            }
            if(!reset)
            {
                for(int i = 0; i < 32; i++)
                {
                    if(i < 16)
                    {
                        verticesColPuzzle[i] = new Color((float) vertRNGPuzzle[i,0]*0.8f,(float) vertRNGPuzzle[i,1]*0.8f,(float) vertRNGPuzzle[i,2]*0.8f);
                    }
                    edgesColPuzzle[i] = new Color((float) edgeRNGPuzzle[i,0]*0.7f,(float) edgeRNGPuzzle[i,1]*0.7f,(float) edgeRNGPuzzle[i,2]*0.7f);
                }
            }
        }
    }


    public int edgeTranslator(int[] dims) //Translates the array to string shit into the ID of the edge.
    {

        string str = string.Join("", new List<int>(dims).ConvertAll(i => i.ToString()).ToArray());
        switch (str)
        {
            case "-1-10-1":
                return 0;
            case "0-11-1":
                return 1;
            case "1-10-1":
                return 2;
            case "0-1-1-1":
                return 3;
            case "-1-101":
                return 4;
            case "0-111":
                return 5;
            case "1-101":
                return 6;
            case "0-1-11":
                return 7;
            case "-1-1-10":
                return 8;
            case "-1-110":
                return 9;
            case "1-110":
                return 10;
            case "1-1-10":
                return 11;
            case "-110-1":
                return 12;
            case "011-1":
                return 13;
            case "110-1":
                return 14;
            case "01-1-1":
                return 15;
            case "-1101":
                return 16;
            case "0111":
                return 17;
            case "1101":
                return 18;
            case "01-11":
                return 19;
            case "-11-10":
                return 20;
            case "-1110":
                return 21;
            case "1110":
                return 22;
            case "11-10":
                return 23;
            case "-10-1-1":
                return 24;
            case "-101-1":
                return 25;
            case "101-1":
                return 26;
            case "10-1-1":
                return 27;
            case "-10-11":
                return 28;
            case "-1011":
                return 29;
            case "1011":
                return 30;
            case "10-11":
                return 31;

            default:
                return -1;
        }
    }

    public void performRotation(int id, int i) //Maps each edge to what it will be after some rotation "id" and sets the next part of the array to that.
    {
        switch(id)
        {
            case 0:
                for(int j = 0; j < 32; j++){
                    rotationLogic[XY[j],i+1]=rotationLogic[j,i];
                }
                return;
            case 1:
                for(int j = 0; j < 32; j++){
                    rotationLogic[XZ[j],i+1]=rotationLogic[j,i];
                }
                return;
            case 2:
                for(int j = 0; j < 32; j++){
                    rotationLogic[XW[j],i+1]=rotationLogic[j,i];
                }
                return;
            case 3:
                for(int j = 0; j < 32; j++){
                    rotationLogic[YZ[j],i+1]=rotationLogic[j,i];
                }
                return;
            case 4:
                for(int j = 0; j < 32; j++){
                    rotationLogic[YW[j],i+1]=rotationLogic[j,i];
                }
                return;
            case 5:
                for(int j = 0; j < 32; j++){
                    rotationLogic[ZW[j],i+1]=rotationLogic[j,i];
                }
                return;
            case 6:
                for(int j = 0; j < 32; j++){
                    rotationLogic[j,i+1]=rotationLogic[XY[j],i];
                }
                return;
            case 7:
                for(int j = 0; j < 32; j++){
                    rotationLogic[j,i+1]=rotationLogic[XZ[j],i];
                }
                return;
            case 8:
                for(int j = 0; j < 32; j++){
                    rotationLogic[j,i+1]=rotationLogic[XW[j],i];
                }
                return;
            case 9:
                for(int j = 0; j < 32; j++){
                    rotationLogic[j,i+1]=rotationLogic[YZ[j],i];
                }
                return;
            case 10:
                for(int j = 0; j < 32; j++){
                    rotationLogic[j,i+1]=rotationLogic[YW[j],i];
                }
                return;
            case 11:
                for(int j = 0; j < 32; j++){
                    rotationLogic[j,i+1]=rotationLogic[ZW[j],i];
                }
                return;

        }
    }

    public void logRots(int[] rots, int[] order) //Easier part of the log, translates the ID's to named rotations for the log.
    {
        string rotString = "";
        string[] rotNames = new string[12] {"XY","XZ","XW","YZ","YW","ZW","YX","ZX","WX","ZY","WY","WZ"};
        for(int i = 0; i < rots.Length; i++)
        {
            rotString += rotNames[rots[i]];
            if(i < 4)
            {
                rotString += ", ";
            }
        }
        Debug.LogFormat("[RGB Hypermaze #{0}] The rotation sequence is {1}", moduleId, rotString);

        string rotOrder = "";
        for(int i = 0; i < order.Length; i++)
        {
            rotOrder += order[i];
            rotOrder += ", ";
        }
        rotOrder += "which is ";
        for(int i = 0; i < order.Length; i++)
        {
            rotOrder += rotNames[rots[order[i]-1]];
            if(i < 3)
            {
                rotOrder += ", ";
            }
        }
        Debug.LogFormat("[RGB Hypermaze #{0}] The rotation order is {1}", moduleId, rotOrder);
    }

    public void logMazes(Color[] verts, Color[] edges) //Aids part of the log, shows the ASCII version of the mazes that hopefully you should see in the log now.
    {
        string[,,] colTranslator = new string[2,2,2] {{{"K","B"},{"G","C"}},{{"R","M"},{"Y","W"}}};
        string[] vertCols = new string[16];
        string[] edgeCols = new string[32];
        string[] passables = new string[32];
        string[] inputPass = new string[32];
        for(int i = 0; i < 16; i++)
        {
            vertCols[i] = colTranslator[(int) Math.Ceiling(verts[i].r),(int) Math.Ceiling(verts[i].g),(int) Math.Ceiling(verts[i].b)];
        }
        for(int i = 0; i < 32; i++)
        {
            edgeCols[i] = colTranslator[(int) Math.Ceiling(edges[i].r),(int) Math.Ceiling(edges[i].g),(int) Math.Ceiling(edges[i].b)];
            if(rotationLogic[i,0] == true)
            {
                passables[i] = "W";
            }
            else
            {
                passables[i] = "P";
            }
            if(rotationLogic[i,4] == true)
            {
                inputPass[i] = "W";
            }
            else
            {
                inputPass[i] = "P";
            }
        }
        Debug.LogFormat("[RGB Hypermaze #{0}] The colors are as follows:ASCII1:\n[RGB Hypermaze #{0}]     {6}—————{22}—————{7}  <-Bottom      {14}—————{34}—————{15}\n[RGB Hypermaze #{0}]    /|          /|     Cube      /|          /|\n[RGB Hypermaze #{0}]   {26} |         {27} |              {38} |         {39} |\n[RGB Hypermaze #{0}]  /  {21}        /  {23}     Top     /  {33}        /  {35}\n[RGB Hypermaze #{0}] {2}———|—{18}—————{3}   |    Cube->  {10}———|—{30}—————{11}   |\n[RGB Hypermaze #{0}] |   |       |   |            |   |       |   |\n[RGB Hypermaze #{0}] |   {5}—————{24}—————{8}            |   {13}—————{36}—————{16}\n[RGB Hypermaze #{0}] {17}  /        {19}  /             {29}  /        {31}  / \n[RGB Hypermaze #{0}] | {25}         | {28}              | {37}         | {40}  \n[RGB Hypermaze #{0}] |/          |/           |   |/          |/   \n[RGB Hypermaze #{0}] {1}—————{20}—————{4}    Y-Edges v   {9}—————{32}—————{12}    \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]                   {46}          {47}                \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]              {42}          {43}                     \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]                   {45}          {48}                \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]              {41}          {44}                     \n[RGB Hypermaze #{0}]                                               ",
                            moduleId,
                            vertCols[0],vertCols[1],vertCols[2],vertCols[3],
                            vertCols[4],vertCols[5],vertCols[6],vertCols[7],
                            vertCols[8],vertCols[9],vertCols[10],vertCols[11],
                            vertCols[12],vertCols[13],vertCols[14],vertCols[15],
                            edgeCols[0],edgeCols[1],edgeCols[2],edgeCols[3],edgeCols[4],edgeCols[5],edgeCols[6],edgeCols[7],
                            edgeCols[8],edgeCols[9],edgeCols[10],edgeCols[11],edgeCols[12],edgeCols[13],edgeCols[14],edgeCols[15],
                            edgeCols[16],edgeCols[17],edgeCols[18],edgeCols[19],edgeCols[20],edgeCols[21],edgeCols[22],edgeCols[23],
                            edgeCols[24],edgeCols[25],edgeCols[26],edgeCols[27],edgeCols[28],edgeCols[29],edgeCols[30],edgeCols[31]);
        Debug.LogFormat("[RGB Hypermaze #{0}] The colored cube's maze is as follows, where P = passage and W = wall:ASCII2:\n[RGB Hypermaze #{0}]     *—————{6}—————*  <-Bottom      *—————{18}—————*\n[RGB Hypermaze #{0}]    /|          /|     Cube      /|          /|\n[RGB Hypermaze #{0}]   {10} |         {11} |              {22} |         {23} |\n[RGB Hypermaze #{0}]  /  {5}        /  {7}     Top     /  {17}        /  {19}\n[RGB Hypermaze #{0}] *———|—{2}—————*   |    Cube->  *———|—{14}—————*   |\n[RGB Hypermaze #{0}] |   |       |   |            |   |       |   |\n[RGB Hypermaze #{0}] |   *—————{8}—————*            |   *—————{20}—————*\n[RGB Hypermaze #{0}] {1}  /        {3}  /             {13}  /        {15}  / \n[RGB Hypermaze #{0}] | {9}         | {12}              | {21}         | {24}  \n[RGB Hypermaze #{0}] |/          |/           |   |/          |/   \n[RGB Hypermaze #{0}] *—————{4}—————*    Y-Edges v   *—————{16}—————*    \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]                   {30}          {31}                \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]              {26}          {27}                     \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]                   {29}          {32}                \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]              {25}          {28}                     \n[RGB Hypermaze #{0}]                                               ",
                            moduleId,
                            passables[0],passables[1],passables[2],passables[3],passables[4],passables[5],passables[6],passables[7],
                            passables[8],passables[9],passables[10],passables[11],passables[12],passables[13],passables[14],passables[15],
                            passables[16],passables[17],passables[18],passables[19],passables[20],passables[21],passables[22],passables[23],
                            passables[24],passables[25],passables[26],passables[27],passables[28],passables[29],passables[30],passables[31]);
        Debug.LogFormat("[RGB Hypermaze #{0}] The input cube's maze is as follows, where P = passage and W = wall:ASCII3:\n[RGB Hypermaze #{0}]     *—————{6}—————*  <-Bottom      *—————{18}—————*\n[RGB Hypermaze #{0}]    /|          /|     Cube      /|          /|\n[RGB Hypermaze #{0}]   {10} |         {11} |              {22} |         {23} |\n[RGB Hypermaze #{0}]  /  {5}        /  {7}     Top     /  {17}        /  {19}\n[RGB Hypermaze #{0}] *———|—{2}—————*   |    Cube->  *———|—{14}—————*   |\n[RGB Hypermaze #{0}] |   |       |   |            |   |       |   |\n[RGB Hypermaze #{0}] |   *—————{8}—————*            |   *—————{20}—————*\n[RGB Hypermaze #{0}] {1}  /        {3}  /             {13}  /        {15}  / \n[RGB Hypermaze #{0}] | {9}         | {12}              | {21}         | {24}  \n[RGB Hypermaze #{0}] |/          |/           |   |/          |/   \n[RGB Hypermaze #{0}] *—————{4}—————*    Y-Edges v   *—————{16}—————*    \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]                   {30}          {31}                \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]              {26}          {27}                     \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]                   {29}          {32}                \n[RGB Hypermaze #{0}]                                               \n[RGB Hypermaze #{0}]              {25}          {28}                     \n[RGB Hypermaze #{0}]                                               ",
                            moduleId,
                            inputPass[0],inputPass[1],inputPass[2],inputPass[3],inputPass[4],inputPass[5],inputPass[6],inputPass[7],
                            inputPass[8],inputPass[9],inputPass[10],inputPass[11],inputPass[12],inputPass[13],inputPass[14],inputPass[15],
                            inputPass[16],inputPass[17],inputPass[18],inputPass[19],inputPass[20],inputPass[21],inputPass[22],inputPass[23],
                            inputPass[24],inputPass[25],inputPass[26],inputPass[27],inputPass[28],inputPass[29],inputPass[30],inputPass[31]);
        string sVertexName = "";
        if(fDCoords[currentVertex].x == -1) { sVertexName += "left-"; } else { sVertexName += "right-"; }
        if(fDCoords[currentVertex].y == -1) { sVertexName += "bottom-"; } else { sVertexName += "top-"; }
        if(fDCoords[currentVertex].z == -1) { sVertexName += "front-"; } else { sVertexName += "back-"; }
        if(fDCoords[currentVertex].w == -1) { sVertexName += "zig"; } else { sVertexName += "zag"; }
        string gVertexName = "";
        if(fDCoords[goalVertex].x == -1) { gVertexName += "left-"; } else { gVertexName += "right-"; }
        if(fDCoords[goalVertex].y == -1) { gVertexName += "bottom-"; } else { gVertexName += "top-"; }
        if(fDCoords[goalVertex].z == -1) { gVertexName += "front-"; } else { gVertexName += "back-"; }
        if(fDCoords[goalVertex].w == -1) { gVertexName += "zig"; } else { gVertexName += "zag"; }
        Debug.LogFormat("RGBHypermaze #{0}] The starting vertex is " + sVertexName + ".", moduleId);
        Debug.LogFormat("RGBHypermaze #{0}] The goal vertex is " + gVertexName + ".", moduleId);
    }

    public void solveAnim(int progress) //Runs the solve anim based on the 4D conversion stuff.
    {
        int[] randomDisplay = new int[4] {rnd.Next(5)+1,rnd.Next(5)+1,rnd.Next(5)+1,rnd.Next(5)+1};
        verticesTf[goalVertex].localScale = new Vector3(0.011f,0.011f,0.011f);
        float subProg = ((float)progress%60)/60;
        Vector4 newCoords = new Vector4(0f,0f,0f,0f);
        for(int i = 0; i < 16; i++)
        {
            if(progress == 60)
            {
                audio.PlaySoundAtTransform("spherememe", transform);
            }
            if(progress < 60)
            {
                if(progress%5 == 0){
                    displayText.text = string.Join("", new List<int>(randomDisplay).ConvertAll(j => j.ToString()).ToArray());
                }
                newCoords = Vector4.Scale(fDCoords[i],new Vector4(1f,1-(subProg*subProg*subProg),1f,1f));
            }
            else if(progress < 120)
            {
                if(progress%5 == 0){
                    displayText.text = string.Join("", new List<int>(randomDisplay).ConvertAll(j => j.ToString()).ToArray());
                }
                newCoords = Vector4.Scale(fDCoords[i],new Vector4(1f,0f,1f,1-(subProg*subProg*subProg)));
            }
            else if(progress < 180)
            {
                if(progress%5 == 0){
                    displayText.text = "N" + randomDisplay[0] + randomDisplay[1] + randomDisplay[2];
                }
                newCoords = Vector4.Scale(fDCoords[i],new Vector4(1f,0f,1-(subProg*subProg*subProg),0f));
            }
            else if(progress < 240)
            {
                if(progress%5 == 0){
                    displayText.text = "NI" + randomDisplay[0] + randomDisplay[1];
                }
                newCoords = Vector4.Scale(fDCoords[i],new Vector4(1-(subProg*subProg*subProg),0f,0f,0f));
            }
            else
            {
                displayText.text = "NIC" + randomDisplay[0];
                float zoom = (float) (3*subProg*subProg*subProg-2.5*subProg*subProg+0.5);
                newCoords = Vector4.Lerp(new Vector4(-15/7f,8/7f,-15/7f,0f),new Vector4(15/7f,-8/7f,15/7f,0f),zoom);
                verticesTf[i].localScale = Vector3.Scale(verticesTf[i].localScale,new Vector3(0.96f,0.96f,0.96f));
            }
            verticesTf[i].localPosition = convert4DTo3D(newCoords.x,newCoords.y,newCoords.z,newCoords.w);
            if(i != goalVertex)
            {
                verticesCol[i].material.color = Color.HSVToRGB(0f,0f,0.8f);
            }
        }
        for(int i = 0; i < 32; i ++)
        {
            edgesTf[i].localPosition = Vector3.Lerp(verticesTf[edgeLeft[i]].localPosition,verticesTf[edgeRight[i]].localPosition,0.5f);
            float d_x = verticesTf[edgeLeft[i]].localPosition.x-verticesTf[edgeRight[i]].localPosition.x;
            float d_y = verticesTf[edgeLeft[i]].localPosition.y-verticesTf[edgeRight[i]].localPosition.y;
            float d_deg = (float) (Math.Atan(d_y/d_x)*180/Math.PI);
            d_deg *= -1;
            edgesTf[i].localRotation = Quaternion.FromToRotation(Vector3.up, verticesTf[edgeLeft[i]].localPosition - verticesTf[edgeRight[i]].localPosition);
            float length = (verticesTf[edgeLeft[i]].localPosition-verticesTf[edgeRight[i]].localPosition).magnitude;

            edgesTf[i].localScale = new Vector3(edgesTf[i].localScale.x, length/2, edgesTf[i].localScale.z);
        }
        if(progress == 299){
            displayText.text = "NICE";
            for(int i = 0; i < 32; i++){
                if(i < 16)
                {
                    verticesTf[i].localScale = new Vector3(0.00001f,0.00001f,0.00001f);
                }
                edgesTf[i].localScale = new Vector3(0.00001f,0.00001f,0.00001f);
            }
            GetComponent<KMBombModule>().HandlePass();
        }
    }

    #pragma warning disable 414
        private readonly string TwitchHelpMessage = @"!{0} go/toggle [presses the display] | !{0} zig-bottom-front-left [presses a vertex]";
    #pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string command)  //Nearly identical to timwi's hypercube code, edited very slightly to match differences in functionality.
    {
        if (Regex.IsMatch(command, @"^\s*(go|activate|stop|run|start|on|off|toggle|display)\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
        {
            if(visualState != inputState && start)
            {
                yield return "sendtochaterror The hypercube is currently changing colors. Please press the display again later.";
            }
            yield return null;
            yield return new[] { display };
            yield break;
        }

        Match m;
        if ((m = Regex.Match(command, string.Format(@"^\s*((?:{0})(?:[- ,;]*(?:{0}))*)\s*$", _dimensionNames.SelectMany(x => x).Join("|")), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)).Success)
        {
            var elements = m.Groups[1].Value.Split(new[] { ' ', ',', ';', '-' }, StringSplitOptions.RemoveEmptyEntries);
            if (elements.Length != 4)
            {
                yield return "sendtochaterror It’s a 4D hypercube, you gotta have 4 dimensions.";
                yield break;
            }
            var dimensions = elements.Select(el => _dimensionNames.IndexOf(d => d.Any(dn => dn.EqualsIgnoreCase(el)))).ToArray();
            var invalid = Enumerable.Range(0, 3).SelectMany(i => Enumerable.Range(i + 1, 3 - i).Where(j => dimensions[i] == dimensions[j]).Select(j => new { i, j })).FirstOrDefault();
            if (invalid != null)
            {
                yield return elements[invalid.i].EqualsIgnoreCase(elements[invalid.j])
                    ? string.Format("sendtochaterror You wrote “{0}” twice.", elements[invalid.i], elements[invalid.j])
                    : string.Format("sendtochaterror “{0}” and “{1}” doesn’t jive.", elements[invalid.i], elements[invalid.j]);
                yield break;
            }
            var vertexIx = 0;
            for (int i = 0; i < 4; i++)
                vertexIx |= _dimensionNames[dimensions[i]].IndexOf(dn => dn.EqualsIgnoreCase(elements[i])) << dimensions[i];
            vertexIx = timwisBetterOrderTranslator[vertexIx];
            yield return null;
            yield return new[] { verticesSel[vertexIx] };
        }
    }
    IEnumerator TwitchHandleForcedSolve() //Literally just enters the solve animation. Lol.
    {
        for(int i = 0; i < 32; i++)
        {
            edgesCol[i].material.color = Color.HSVToRGB(0f,0f,0.6f);
        }
        verticesCol[goalVertex].material.color = new Color(0f,0.8f,0f);
        start = true;
        moduleSolved = true;
        inputState = true;
        visualState = true;
        yield return true;
    }
}
