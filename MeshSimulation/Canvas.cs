using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace MeshSimulation
{
    public class Source
    {
        public Cell SourceCell;
        public double SourcePressure;

        public Source(Cell c, double p)
        {
            SourceCell = new Cell(0, 0);
            SourceCell = c;
            SourcePressure = p;
            SourceCell.Phase = 0;
            SourceCell.Volume = 1;
        }
    }

    public class Canvas
    {
        public int Width;
        public int Height;
        public double Step;
        public List<List<Cell>> CellMesh;

        public List<Source> SourceList;

        public List<List<bool>> isDiscovered;
        public List<Bubble> BubbleList;
        public List<Drop> DropList;
        public double SumGasAmount;
        
        public Canvas()
        {
            Width = 0;
            Height = 0;
            Step = 0;
            CellMesh = new List<List<Cell>>();

            SourceList = new List<Source>();

            isDiscovered = new List<List<bool>>();
            BubbleList = new List<Bubble>();
            DropList = new List<Drop>();
            SumGasAmount = 0;
        }

        public void createEmptyMesh(int width = 0, int height = 0, double step = 0)
        {//初始化
            Width = width;
            Height = height;
            Step = step;
            if (width > 0 && height > 0)
            {
                for (int i = 0; i < Width; i++)
                {
                    List<Cell> column = new List<Cell>();
                    List<bool> columnOfIsDiscovered = new List<bool>();
                    for (int j = 0; j < Height; j++)
                    {
                        Cell c = new Cell(i, j);
                        column.Add(c);
                        columnOfIsDiscovered.Add(false);
                    }
                    CellMesh.Add(column);
                    isDiscovered.Add(columnOfIsDiscovered);
                }
            }
        }

        public void initializeMesh(GH_Structure<GH_Integer> referVolumeMesh)
        {
            double SumGas = 0;
            int nx = referVolumeMesh.Branches.Count - 1;
            int ny = referVolumeMesh.Branches[0].Count - 1;
            for (int i = 0; i < nx && i < Width; i++)
            {
                for (int j = 0; j < ny && j < Height; j++)
                {
                    //读图，待修改
                    int v = referVolumeMesh.Branches[i][j].Value;
                    if(v == 1)
                    {//gas
                        CellMesh[i][j].Volume = 1;
                        CellMesh[i][j].Phase = 1;
                        SumGas += 1;
                    }
                    else if(v == 2)
                    {//liquid
                        CellMesh[i][j].Volume = 1;
                        CellMesh[i][j].Phase = 0;
                    }
                }
            }
            SumGasAmount = SumGas;
        }

        public void loadLiquidSource(List<Point3d> sourcePoint, List<double> sourcePressure)
        {
            SourceList.Clear();
            if(sourcePoint.Count == sourcePressure.Count)
            {
                int n = sourcePoint.Count;
                for(int i = 0; i < n; i++)
                {
                    Point3d sc = sourcePoint[i];
                    int x = (int)(sc.X / Step);
                    int y = (int)(sc.Y / Step);
                    if (CellMesh[x][y].Phase > 0) SumGasAmount--;
                    Source source = new Source(CellMesh[x][y], sourcePressure[i]);
                    SourceList.Add(source);
                }
            }
        }

        public void updateLiquidSourcePressure(List<double> sourcePressure)
        {
            if(sourcePressure.Count == SourceList.Count)
            {
                int n = SourceList.Count;
                for(int i = 0; i < n; i++)
                {
                    SourceList[i].SourcePressure = sourcePressure[i];
                }
            }
        }
        public List<List<int>> displayMesh()
        {
            List<List<int>> display = new List<List<int>>();
            for (int i = 0; i < Width; i++)
            {
                List<int> column = new List<int>();
                for (int j = 0; j < Height; j++)
                {
                    column.Add(-1);//default
                    if (CellMesh[i][j].Volume == 0) column[j] = 0;//wall
                    else
                    {
                        if (CellMesh[i][j].Phase <= 0) column[j] = 2;//liquid
                        else if (CellMesh[i][j].Phase > 0) column[j] = 1;//gas
                    }
                }
                display.Add(column);
            }
            return display;
        }

        public List<Color> displayMeshColor()
        {
            List<Color> display = new List<Color>();
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    display.Add(Color.Black);//default
                    int index = i * Height + j;
                    if (CellMesh[i][j].Volume == 0) display[index] = Color.Gray;//wall
                    else
                    {
                        if (CellMesh[i][j].Phase <= 0) display[index] = Color.Blue;//liquid
                        else if (CellMesh[i][j].Phase > 0) display[index] = Color.White;//gas
                    }
                }
            }
            return display;
        }
        public List<GH_Mesh> displayMeshColored()
        {
            List<GH_Mesh> display = new List<GH_Mesh>();
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    Interval xInt = new Interval(i * Step, (i + 1) * Step);
                    Interval yInt = new Interval(j * Step, (j + 1) * Step);
                    Mesh m = Mesh.CreateFromPlane(Rhino.Geometry.Plane.WorldXY, xInt, yInt, 1, 1);
                    if (CellMesh[i][j].Volume == 0) m.VertexColors.CreateMonotoneMesh(Color.Gray);
                    else if (CellMesh[i][j].Phase > 0) m.VertexColors.CreateMonotoneMesh(Color.White);
                    else if (CellMesh[i][j].Phase <= 0) m.VertexColors.CreateMonotoneMesh(Color.Blue);
                    else m.VertexColors.CreateMonotoneMesh(Color.Black);

                    display.Add(new GH_Mesh(m));
                }
            }
            return display;
        }

        public void readMesh()
        {
            refreshIsDiscoveredMap();
            BubbleList.Clear();
            DropList.Clear();

            foreach (Source source in SourceList)
            {
                Cell currentCell = source.SourceCell;
                List<Cell> cellGroup = searchGroup(ref currentCell);
                DropList.Add(new Drop(cellGroup, source.SourcePressure));
            }

            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (!isDiscovered[i][j]) {
                        if (CellMesh[i][j].Volume == 0) isDiscovered[i][j] = true;
                        else
                        {
                            Cell currentCell = CellMesh[i][j];
                            List<Cell> cellGroup = searchGroup(ref currentCell);
                            if (currentCell.Phase > 0)
                            {//gas
                                BubbleList.Add(new Bubble(cellGroup));
                            }
                            else
                            {//drop
                                DropList.Add(new Drop(cellGroup));
                            }
                        }
                    }
                }
            }
            SumGasAmount = analyzeBubles();
            analyzeDrops();
        }

        private double analyzeBubles()
        {
            double SumGas = 0;
            int numberOfBubbles = BubbleList.Count;
            for (int j = 0; j < numberOfBubbles; j++)
            {
                Bubble bubble = BubbleList[j];
                int n = bubble.CellList.Count;
                for (int i = 0; i < n; i++)
                {
                    bubble.SumGasAmount += bubble.CellList[i].Phase;
                    SumGas += bubble.CellList[i].Phase;
                    bubble.SumVolume += bubble.CellList[i].Volume;
                    bubble.CellList[i].index = j;//update index for every cell
                }
                double evenGasAmount = BubbleList[j].SumGasAmount / n;
                for (int i = 0; i < n; i++)
                {
                    bubble.CellList[i].Phase = evenGasAmount;//update phase
                }
                ///p=N/V (p0=1)
                bubble.Pressure = bubble.SumGasAmount / BubbleList[j].SumVolume;
            }
            return SumGas;
        }

        private void analyzeDrops()
        {
            int numberOfDrops = DropList.Count;
            for (int j = 0; j < numberOfDrops; j++)
            {
                Drop drop = DropList[j];
                int n = drop.CellList.Count;
                double pressure = 0;

                for (int i = 0; i < n; i++)
                {
                    Cell c = drop.CellList[i];
                    c.index = j;//update index for every cell

                    List<Cell> neighbour = getNeighbour(c);

                    foreach (Cell nc in neighbour)
                    {
                        if (nc.Phase > 0)
                        {//气液界面
                            Interface interf = new Interface(c, nc);
                            drop.InterfaceList.Add(interf);
                            if(BubbleList[nc.index].Pressure > pressure)
                            {
                                pressure = BubbleList[nc.index].Pressure;
                            }
                            //pressure += BubbleList[nc.index].Pressure;
                        }
                    }
                }
                //气液界面按照体积降序排列
                int numberOfInterface = drop.InterfaceList.Count;
                List<Interface> orderedInterfaceList = new List<Interface>();
                List<bool> isOrdered = new List<bool>();
                for(int i = 0; i < numberOfInterface; i++)
                {
                    isOrdered.Add(false);
                }
                for (int i = 0; i < numberOfInterface; i++)
                {
                    int maxV = 0;
                    int maxIndex = 0;
                    for(int k = 0; k < numberOfInterface; k++)
                    {
                        Interface currentItf = drop.InterfaceList[k];
                        int v = BubbleList[currentItf.BubbleIndex].SumVolume;
                        if (v > maxV && isOrdered[k] == false)
                        {
                            maxV = v;
                            maxIndex = k;
                        }
                    }
                    isOrdered[maxIndex] = true;
                    orderedInterfaceList.Add(drop.InterfaceList[maxIndex]);
                }
                drop.InterfaceList = orderedInterfaceList;

                if (!drop.isSource && drop.InterfaceList.Count != 0)
                {
                    /*pressure /= drop.InterfaceList.Count;
                    drop.Pressure = pressure;*/
                    drop.Pressure = pressure * 0.95;
                }
            }
        }
        
        private void refreshIsDiscoveredMap()
        {
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    isDiscovered[i][j] = false;
                }
            }

        }
        
        public List<Cell> searchGroup(ref Cell startCell)
        {
            List<Cell> group = new List<Cell>();
            group.Add(startCell);

            List<Cell> queue = new List<Cell>();//BFS
            queue.Add(startCell);

            int startCellVolume = startCell.Volume;
            double startCellGasAmout = startCell.Phase;
            isDiscovered[startCell.X_cell][startCell.Y_cell] = true;

            while (queue.Count > 0)
            {
                Cell c = queue[0];
                queue.RemoveAt(0);
                List<Cell> neighbour = getNeighbour(c);

                foreach (Cell nc in neighbour)
                {
                    if (isDiscovered[nc.X_cell][nc.Y_cell] == false && nc.Volume > 0) {
                        if ((startCell.Phase > 0 && nc.Phase > 0) || //gas
                            (startCell.Phase == 0 && nc.Phase == 0))//liquid
                        {
                            isDiscovered[nc.X_cell][nc.Y_cell] = true;
                            queue.Add(nc);
                            group.Add(nc);
                        }
                    }

                }
            }

            return group;

        }

        public List<List<int>> displayCellList(List<Cell> cellList)
        {
            List<List<int>> display = new List<List<int>>();
            for (int i = 0; i < Width; i++)
            {
                List<int> column = new List<int>();
                for (int j = 0; j < Height; j++)
                {
                    column.Add(-1);//default
                }
                display.Add(column);
            }
            foreach(Cell c in cellList)
            {
                display[c.X_cell][c.Y_cell] = 1;
            }
            return display;
        }


        public void updateFluidDistribution()
        {
            foreach(Drop drop in DropList)
            {
                if (drop.isSource)
                {
                    int numberOfInterface = drop.InterfaceList.Count;
                    for (int i = 0; i < numberOfInterface; i++)
                    {
                        int action = interfaceAction(drop.InterfaceList[i]);
                        takeAction(drop.InterfaceList[i], action);
                    }
                }
                else
                {//前进后退相匹配着排序
                    int numberOfInterface = drop.InterfaceList.Count;
                    List<int> forwardInterfaceIndexList = new List<int>();
                    List<int> backwardInterfaceIndexList = new List<int>();

                    for (int i = 0; i < numberOfInterface; i++)
                    {
                        int action = interfaceAction(drop.InterfaceList[i]);
                        if (action == 1) forwardInterfaceIndexList.Add(i);
                        else if (action == -1) backwardInterfaceIndexList.Add(i);
                    }
                    int nf = forwardInterfaceIndexList.Count;
                    int nb = backwardInterfaceIndexList.Count;
                    int nn = Math.Min(nf, nb);

                    for (int i = 0; i < nn; i++)
                    {
                        takeAction(drop.InterfaceList[forwardInterfaceIndexList[i]], 1);
                        takeAction(drop.InterfaceList[backwardInterfaceIndexList[i]], -1);
                    }
                }
            }
        }

        private int interfaceAction(Interface itf)
        {
            Cell liquidCell = itf.LiquidCell;
            Cell gasCell = itf.GasCell;
            double liquidPressure = DropList[liquidCell.index].Pressure;
            int gasVolume = BubbleList[gasCell.index].SumVolume;
            double gasAmount = BubbleList[gasCell.index].SumGasAmount;
            //double gasPressure = BubbleList[gasCell.index].Pressure;
            int action = 0;

            if (gasVolume > 1 && liquidPressure >= gasAmount / (gasVolume - 1))
            {
                action = 1;
            }
            else if (liquidPressure < gasAmount / gasVolume)
            {
                action = -1;
                foreach(Source s in SourceList)
                {
                    if(s.SourceCell == liquidCell)
                    {
                        action = 0;
                    }
                }
            }
            else if(gasVolume == 1 && liquidPressure >= gasAmount * 1.5)
            {
                action = 2;
            }

            return action;
        }

        private void takeAction(Interface itf, int action)
        {
            Cell gasCell = itf.GasCell;
            Cell liquidCell = itf.LiquidCell;
            if (action == 1)
            {
                List<Cell> neighbour = getNeighbour(gasCell);
                List<Cell> gasNeighbour = new List<Cell>();
                foreach(Cell nc in neighbour)
                {
                    if (nc.Phase > 0) gasNeighbour.Add(nc);
                }
                if(gasNeighbour.Count > 0)
                {
                    double addtion = gasCell.Phase / gasNeighbour.Count;
                    foreach (Cell gnc in gasNeighbour)
                    {
                        gnc.Phase += addtion;
                    }
                    gasCell.Phase = 0;//update phase
                }

            }
            else if(action == -1)
            {
                liquidCell.Phase += gasCell.Phase / 2;//update phase
                gasCell.Phase -= gasCell.Phase / 2;
            }
            else if(action == 2)
            {
                gasCell.Phase = 0;
            }
        }

        private List<Cell> getNeighbour(Cell c)
        {
            int currentX = c.X_cell;
            int currentY = c.Y_cell;
            List<Cell> neighbour = new List<Cell>();
            if (currentX > 0) neighbour.Add(CellMesh[currentX - 1][currentY]);
            if (currentY > 0) neighbour.Add(CellMesh[currentX][currentY - 1]);
            if (currentX < Width - 1) neighbour.Add(CellMesh[currentX + 1][currentY]);
            if (currentY < Height - 1) neighbour.Add(CellMesh[currentX][currentY + 1]);
            return neighbour;
        }

        public void duplicateCellMeshFrom(List<List<Cell>> cm)
        {
            for(int i = 0; i < Width; i++)
            {
                for(int j = 0; j < Height; j++)
                {
                    CellMesh[i][j].Phase = cm[i][j].Phase;
                }
            }
        }

        public bool isSameCellMeshAs(List<List<Cell>> cm)
        {
            for (int i = 0; i < Width; i++)
            {
                for (int j = 0; j < Height; j++)
                {
                    if (CellMesh[i][j].Phase == 0 && cm[i][j].Phase > 0) return false;
                    else if (CellMesh[i][j].Phase > 0 && cm[i][j].Phase == 0) return false;
                }
            }
            return true;
        }
    }
}