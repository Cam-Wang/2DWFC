using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public class WFCManager : MonoBehaviour
{
    //105476811 seed breaks current gen on a 30x30 map
    public bool useSeed = true;
    public int seed;
    [SerializeField]
    private TilemapManager manager;

    [SerializeField]
    private Module[] _modules;

    [SerializeField]
    private int _width,_height;

    #region WFC Data Structures
    bool[,] _hasBeenAssigned,_hasBeenChecked;
    int[,] _finalMap;

    ModuleCell[,] _modulePossibilityMap;
    int[][][] _possibilityMap;

    int[] dx = new int[] {-1,0,0,1};
    int[] dy = new int[] {0,-1,1,0};
    private Queue<Vector2Int> propogationQueue;
    #endregion


    void Start()
    {
        if(useSeed)
        {
            Random.InitState(seed);
        }
        else
        {
            seed = (int)System.DateTime.Now.Ticks;
            Random.InitState(seed);
        }

        _hasBeenAssigned = new bool[_width,_height];
        _hasBeenChecked = new bool[_width,_height];
        _finalMap = new int[_width,_height];
        propogationQueue = new Queue<Vector2Int>();
        _modulePossibilityMap = new ModuleCell[_width,_height];
        for(int i = 0;i<_width;i++)
        {
            for(int j = 0;j<_height;j++)
            {
                _modulePossibilityMap[i,j] = new ModuleCell();
            }
        }
        Debug.Log(_modulePossibilityMap[3,3].totalPossibleWeight);
        InitFinalMap();
        InitPossibilityMap();
        resetHasBeenChecked();

        int x = Random.Range(0,_width);
        int y = Random.Range(0,_height);
        int t = Random.Range(0,_modules.Length - 1);
        //DebugPossiblityMap();
        Assign(new Vector2Int(x,y),t);
        
        // while(Collapse());
        DrawMap();

    }
    void Update()
    {
        // if(Input.anyKeyDown)
        // {
        //     Collapse();
        //     DrawMap();
        // }
    }
    private void Assign(Vector2Int pos, int moduleID)
    {
        _hasBeenAssigned[pos.x,pos.y] = true;
        _finalMap[pos.x,pos.y] = moduleID;
        _possibilityMap[pos.x][pos.y] = new int[] {moduleID};
        Debug.Log("Assigned Tile: " + pos.y + " " + pos.x + " to module " + (ModuleIDS)moduleID);
        //Add all neighbors to a stack or queue
        AddNeighbors(pos);
        //Then call propogate
        //have to propogate after assigning
        Propogate();
    }

    private void AddNeighbors(Vector2Int pos)
    {
        for(int i = 0;i < dx.Length;i++)
        {
            Vector2Int potentialNeighbor = new Vector2Int(pos.x + dx[i],pos.y + dy[i]);
            if(ValidateCoordinate(potentialNeighbor) && !_hasBeenChecked[potentialNeighbor.x,potentialNeighbor.y] && !_hasBeenAssigned[potentialNeighbor.x,potentialNeighbor.y])
            {
                propogationQueue.Enqueue(potentialNeighbor);
            }
        }
    }
    private void Propogate()
    {
        //Debug.Log("Begin Propogation");
        resetHasBeenChecked();
        while(propogationQueue.Count > 0)
        {
            Vector2Int currentCell = propogationQueue.Dequeue();
            if(_hasBeenChecked[currentCell.x,currentCell.y])
                continue;
            _hasBeenChecked[currentCell.x,currentCell.y] = true;
            Constrain(currentCell);
            AddNeighbors(currentCell);
        }
        //Debug.Log("End Propogation");
    }
    private void Constrain(Vector2Int pos)
    {
        //Take current position look at all neighbors
        //For each neighbor depending on direction create a new list of all possible modules that could border that cell for each given possible module
        //Change the possibleMatrix array to reflect that new list
        //PossibleModuleIDsForPos should contain all posisble modules to start and if a given module is not in a given neighbours potential neighbour modules remove from the list
        
        //Creates a list with all possible Modules
        List<int> possibleModuleIDsForPos = new List<int>();
        for(int i = 0;i < _modules.Length;i++)
        {
            possibleModuleIDsForPos.Add(i);
        }

        //Iterates over each neighbour and checks if neighbor is in bounds
        for(int i = 0;i < dx.Length;i++)
        {
            List<int> neighborsPotentialModuleIDS = new List<int>();
            Vector2Int potentialNeighbor = new Vector2Int(pos.x + dx[i],pos.y + dy[i]);
            if(ValidateCoordinate(potentialNeighbor))
            {
                int[] possibleModuleIDsforNeighbor = GetPotentialModules(potentialNeighbor);
                //Checks neighbors relative location to pos
                if(dx[i] != 0)
                {
                    if(dx[i] < 0)
                    {
                        //Neighbor is above looking at down connections
                        foreach(int moduleID in possibleModuleIDsforNeighbor)
                        {
                            foreach(int connectionID in _modules[moduleID].upConnections)
                            {
                                if(!neighborsPotentialModuleIDS.Contains(connectionID))
                                    neighborsPotentialModuleIDS.Add(connectionID);
                            }
                        }
                    }
                    else
                    {
                        //Neighbor is below looking at up connections                        
                        foreach(int moduleID in possibleModuleIDsforNeighbor)
                        {
                            foreach(int connectionID in _modules[moduleID].downConnections)
                            {
                                if(!neighborsPotentialModuleIDS.Contains(connectionID))
                                    neighborsPotentialModuleIDS.Add(connectionID);
                            }
                        }
                    }
                }
                else
                {
                    if(dy[i] < 0)
                    {
                        //Neighbor is to the left looking at right connections
                        foreach(int moduleID in possibleModuleIDsforNeighbor)
                        {
                            foreach(int connectionID in _modules[moduleID].rightConnections)
                            {
                                if(!neighborsPotentialModuleIDS.Contains(connectionID))
                                    neighborsPotentialModuleIDS.Add(connectionID);
                            }
                        }
                    }
                    else
                    {
                        //Neighbor is to the right looking at left connections
                        foreach(int moduleID in possibleModuleIDsforNeighbor)
                        {
                            foreach(int connectionID in _modules[moduleID].leftConnections)
                            {
                                if(!neighborsPotentialModuleIDS.Contains(connectionID))
                                    neighborsPotentialModuleIDS.Add(connectionID);
                            }
                        }
                    }
                }
                IEnumerable<int> differnce = possibleModuleIDsForPos.Except(neighborsPotentialModuleIDS);
                foreach(int moduleID in differnce.ToList())
                {
                    possibleModuleIDsForPos.Remove(moduleID);
                }
            }

        }
        possibleModuleIDsForPos.Sort();
        _possibilityMap[pos.x][pos.y] = possibleModuleIDsForPos.ToArray();
    }
    private bool Collapse()
    {
        //Scan the enitre board and find the lowest 'height' aka length of a given array at x,y
        //Later down the line we can tweek how the selection for the next cell to collapse works
        //Some quick ideas is completley random or based on distance from last cell collapsed
        //For now we will do a random cell

        //Going to create the new entropy data
        int minHeight = _modules.Length + 1;
        List<Vector2Int> lowestCells = new List<Vector2Int>();

        //Generates a List of Vector2Ints that have the minimum height
        for(int i = 0; i < _width;i++)
        {
            for(int j = 0; j < _height; j++)
            {
                int height = _possibilityMap[i][j].Length;
                if(height < minHeight && !_hasBeenAssigned[i,j] && height > 0)
                {
                    minHeight = height;
                    lowestCells.Clear();
                    lowestCells.Add(new Vector2Int(i,j));
                }
                if(height == minHeight)
                {
                    lowestCells.Add(new Vector2Int(i,j));
                }
            }
        }
        if(minHeight == _modules.Length +1)
        {
            return false;
        }
        else
        {
            //Select Random Vector2Int and Assign Random Module from possible Modules
            int index = Random.Range(0,lowestCells.Count());
            Vector2Int position = lowestCells[index];
            
            int[] possibleModules = _possibilityMap[position.x][position.y];
            DebugArray(possibleModules);
            
            index = Random.Range(0,possibleModules.Length);
            //Debug.Log("Pos: " + position.x + " " + position.y + " Tile: " + index);
            int moduleID = possibleModules[index];
            Assign(position,moduleID);
            return true;
        }
    }
    private int[] GetPotentialModules(Vector2Int pos)
    {
        return _possibilityMap[pos.x][pos.y];
    }
    private bool ValidateCoordinate(Vector2Int pos)
    {
        if(pos.x < 0 || pos.x >= _width || pos.y < 0 || pos.y >= _height)
        {
            return false;
        }
        return true;
    }
    private void DrawMap()
    {
        for(int i = 0; i < _width;i++)
        {
            for(int j = 0; j < _height;j++)
            {
                int _moduleID = _finalMap[i,j];
                if(_moduleID > -1)
                {
                    manager.SetTile(new Vector3Int(j,i,0),_modules[_moduleID]);
                }
            }
        }
    }
    private void InitFinalMap()
    {
        for(int i = 0; i < _width;i++)
        {
            for(int j = 0; j < _height;j++)
            {
                _finalMap[i,j] = -1;
            }
        }
    }
    private void InitPossibilityMap()
    {  
        int[] defaultPossibility = new int[_modules.Length];
        for(int i = 0; i < _modules.Length;i++)
        {
            defaultPossibility[i] = i;
        }
        _possibilityMap = new int[_width][][];
        for(int i = 0;i<_width;i++)
        {
            _possibilityMap[i] = new int[_height][];

            for(int j = 0;j < _height; j ++)
            {
                _possibilityMap[i][j] = defaultPossibility;
            }
        }

    }
    private void resetHasBeenChecked()
    {
        for(int i = 0;i < _width;i++)
        {
            for(int j = 0;j < _height;j++)
            {
                _hasBeenChecked[i,j] = _hasBeenAssigned[i,j];
            }
        }
    }
    private void DebugPossiblityMap()
    {
        for(int i = 0;i < _width;i++)
        {
            string temp = "{ ";
            for(int j =0; j< _height;j++)
            {
                temp += "{ ";
                foreach(int v in _possibilityMap[i][j])
                {
                    temp+= v + " ";
                }
                temp += "},";
            }
            temp += "}";
            Debug.Log(temp);
        }
    }
    private void DebugArray(int[] arr)
    {
        string temp = "{";
        foreach(int v in arr)
        {
            temp += (ModuleIDS)v + " ";
        }
        temp += "}";
        Debug.Log(temp);
    }
}

public class ModuleCell
{
    public List<int> possibleModules;

    public int totalPossibleWeight;

    public float totalPossibleLogWeight;

    public static int[] moduleFreq = {50,8,13,9,6,6,2,2,3,2};

    public ModuleCell()
    {
        possibleModules = new List<int>();
        for(int i = 0; i< moduleFreq.Length;i++)
        {
            possibleModules.Add(i);
        }
        foreach(int i in moduleFreq)
        {
            totalPossibleWeight += i;
            totalPossibleLogWeight += i * Mathf.Log(i,7);
        }
    }

    public void RemoveModule(int moduleID)
    {
        if(possibleModules.Contains(moduleID))
        {
            possibleModules.Remove(moduleID);
            int freq = moduleFreq[moduleID];
            totalPossibleWeight -= freq;
            totalPossibleLogWeight -= freq * Mathf.Log(freq,7);
        }
    }
    
}
