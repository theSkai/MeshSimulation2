using Grasshopper.Kernel.Types;
using Grasshopper.Kernel.Types.Transforms;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshSimulation
{

    public class Source3D
    {
        public Cell3D SourceCell;
        public double SourcePressure;

        public Source3D(Cell3D c, double p)
        {
            SourceCell = new Cell3D(0, 0, 0);
            SourceCell = c;
            SourcePressure = p;
            SourceCell.Phase = 0;
            SourceCell.Volume = 1;
        }
    }

    public class Canvas3D
    {
        public int X_extent;
        public int Y_extent;
        public int Z_extent;

        public double Step;
        //public List<List<List<Cell3D>>> CellMesh;//x,y,z

        public List<Cell3D> VesselCellList;//x,y,z
        public int VesselCellCount;
        public List<Source3D> SourceList;//liquid source
        public List<Bubble3D> BubbleList;
        public List<Drop3D> DropList;

        public double SumGasAmount;

        public Canvas3D()
        {
            X_extent = 0;
            Y_extent = 0;
            Z_extent = 0;
            Step = 0;

            //CellMesh = new List<List<List<Cell3D>>>();

            VesselCellList = new List<Cell3D>();
            VesselCellCount = 0;

            SourceList = new List<Source3D>();
            BubbleList = new List<Bubble3D>();
            DropList = new List<Drop3D>();

            SumGasAmount = 0;
        }
        public void InitializeVesselCellListFrom(List<Cell3D> cm, bool ifDubpicated)
        {
            if (ifDubpicated)
            {
                VesselCellCount = cm.Count;
                VesselCellList.Clear();
                for (int i = 0; i < VesselCellCount; i++)
                {
                    VesselCellList.Add(new Cell3D(0, 0, 0));
                }
            }
            else
            {
                VesselCellCount = cm.Count;
                VesselCellList = cm;//引用
                SumGasAmount = 0;
                for (int i = 0; i < VesselCellCount; i++)
                {
                    cm[i].Phase = 1;//初始态，不支持放置初始液体
                    SumGasAmount += cm[i].Volume;
                }
            }
        }

        public void UpdateVesselCellListFrom(List<Cell3D> cm)
        {
            for (int i = 0; i < VesselCellCount; i++)
            {
                VesselCellList[i].Phase = cm[i].Phase;
            }
        }

        public bool IsSameVesselCellListAs(List<Cell3D> cm)
        {
            for (int i = 0; i < VesselCellCount; i++)
            {
                if (VesselCellList[i].Phase == 0 && cm[i].Phase > 0) return false;
                else if (VesselCellList[i].Phase > 0 && cm[i].Phase == 0) return false;
            }
            return true;
        }
        public void LoadLiquidSource(List<Point3d> sourcePoint, List<double> sourcePressure)
        {
            if (sourcePoint.Count != sourcePressure.Count) return;

            SourceList.Clear();
            int n = sourcePoint.Count;
            for (int j = 0; j < VesselCellCount; j++)
            {
                for (int i = 0; i < n; i++)
                {
                    Point3d sc = sourcePoint[i];
                    int x = (int)(sc.X / Step);
                    int y = (int)(sc.Y / Step);
                    int z = (int)(sc.Z / Step);

                    if(x == VesselCellList[j].X_coor &&
                       y == VesselCellList[j].Y_coor &&
                       z == VesselCellList[j].Z_coor)
                    {
                        if (VesselCellList[j].Phase > 0) SumGasAmount -= 1;
                        SourceList.Add(new Source3D(VesselCellList[j], sourcePressure[i]));
                    }
                }
            }
        }

        public void UpdateLiquidSourcePressure(List<double> sourcePressure)
        {
            if (sourcePressure.Count == SourceList.Count)
            {
                int n = SourceList.Count;
                for (int i = 0; i < n; i++)
                {
                    SourceList[i].SourcePressure = sourcePressure[i];
                }
            }
        }
        
        public List<GH_Mesh> DisplayMeshColored()
        {
            List<GH_Mesh> Display = new List<GH_Mesh>();
            Point3d min = new Point3d(0, 0, 0);
            Point3d max = new Point3d(1, 1, 1);
            BoundingBox box = new BoundingBox(min, max);
            Mesh baseMesh = Mesh.CreateFromBox(box, 1, 1, 1);

            for (int i = 0; i < VesselCellCount; i++)
            {
                Cell3D currentCell = VesselCellList[i];

                Mesh newMesh = baseMesh.DuplicateMesh();
                newMesh.Translate(currentCell.X_coor * Step, currentCell.Y_coor * Step, currentCell.Z_coor * Step);

                if (currentCell.Phase > 0) newMesh.VertexColors.CreateMonotoneMesh(Color.FromArgb(40, 255, 255, 255));
                else newMesh.VertexColors.CreateMonotoneMesh(Color.FromArgb(100, 0, 0, 255));

                Display.Add(new GH_Mesh(newMesh));
            }
            return Display;
        }

        
        public void ReadMesh()
        {
            for(int i = 0; i < VesselCellCount; i++)
            {
                VesselCellList[i].IsChecked = false;
            }
            BubbleList.Clear();
            DropList.Clear();

            foreach (Source3D source in SourceList)
            {
                Cell3D currentCell = source.SourceCell;
                List<Cell3D> cellGroup = SearchGroup(ref currentCell);
                DropList.Add(new Drop3D(cellGroup, source.SourcePressure));
            }

            for(int i = 0; i < VesselCellCount; i++)
            {
                Cell3D currentCell = VesselCellList[i];
                if (currentCell.IsChecked == false)
                {
                    List<Cell3D> cellGroup = SearchGroup(ref currentCell);
                    if (currentCell.Phase > 0)
                    {//gas
                        BubbleList.Add(new Bubble3D(cellGroup));
                    }
                    else
                    {//drop
                        DropList.Add(new Drop3D(cellGroup));
                    }
                }
            }

            SumGasAmount = AnalyzeBubles();
            AnalyzeDrops();
        }

        private double AnalyzeBubles()
        {
            double sumGasAmount = 0;
            int numberOfBubbles = BubbleList.Count;
            for (int j = 0; j < numberOfBubbles; j++)
            {
                Bubble3D currentBubble = BubbleList[j];
                int n = currentBubble.CellList.Count;
                for (int i = 0; i < n; i++)
                {
                    currentBubble.BubbleGasAmount += currentBubble.CellList[i].Phase;
                    sumGasAmount += currentBubble.CellList[i].Phase;

                    currentBubble.BubbleVolume += currentBubble.CellList[i].Volume;
                    currentBubble.CellList[i].Index = j;//update index for every cell
                }
                double evenGasAmount = BubbleList[j].BubbleGasAmount / n;
                for (int i = 0; i < n; i++)
                {
                    currentBubble.CellList[i].Phase = evenGasAmount;//update phase
                }
                ///p=N/V (p0=1)
                currentBubble.Pressure = currentBubble.BubbleGasAmount / BubbleList[j].BubbleVolume;
            }
            return sumGasAmount;
        }

        private void AnalyzeDrops()
        {
            int numberOfDrops = DropList.Count;
            for (int j = 0; j < numberOfDrops; j++)
            {
                Drop3D currentDrop = DropList[j];
                int n = currentDrop.CellList.Count;
                double pressure = 0;

                for (int i = 0; i < n; i++)
                {
                    Cell3D c = currentDrop.CellList[i];
                    c.Index = j;//update index for every cell

                    List<Cell3D> neighbour = c.Neighbour;

                    foreach (Cell3D nc in neighbour)
                    {
                        if (nc.Phase > 0)
                        {//气液界面
                            Interface3D interf = new Interface3D(c, nc);//(liquid, gas)
                            currentDrop.InterfaceList.Add(interf);
                            if (BubbleList[nc.Index].Pressure > pressure)
                            {//获得与该drop接触的bubble中最大的气压值
                                pressure = BubbleList[nc.Index].Pressure;
                            }
                            //pressure += BubbleList[nc.index].Pressure;
                        }
                    }
                }
                //气液界面按照bubble体积降序排列
                int numberOfInterface = currentDrop.InterfaceList.Count;
                List<Interface3D> orderedInterfaceList = new List<Interface3D>();
                List<bool> isOrdered = new List<bool>();
                for (int i = 0; i < numberOfInterface; i++)
                {
                    isOrdered.Add(false);
                }
                for (int i = 0; i < numberOfInterface; i++)
                {
                    double maxV = 0;
                    int maxIndex = 0;
                    for (int k = 0; k < numberOfInterface; k++)
                    {
                        Interface3D currentItf = currentDrop.InterfaceList[k];
                        double v = BubbleList[currentItf.BubbleIndex].BubbleVolume;
                        if (v > maxV && isOrdered[k] == false)
                        {
                            maxV = v;
                            maxIndex = k;
                        }
                    }
                    isOrdered[maxIndex] = true;
                    orderedInterfaceList.Add(currentDrop.InterfaceList[maxIndex]);
                }
                currentDrop.InterfaceList = orderedInterfaceList;

                if (!currentDrop.isSource && currentDrop.InterfaceList.Count != 0)
                {
                    //pressure /= currentDrop.InterfaceList.Count;
                    //currentDrop.Pressure = pressure;
                    currentDrop.Pressure = pressure * 0.95;//最大气压的0.95
                }
            }
        }

        public List<Cell3D> SearchGroup(ref Cell3D startCell)
        {
            List<Cell3D> group = new List<Cell3D>();
            group.Add(startCell);

            List<Cell3D> queue = new List<Cell3D>();//BFS
            queue.Add(startCell);

            startCell.IsChecked = true;

            while (queue.Count > 0)
            {
                Cell3D c = queue[0];
                queue.RemoveAt(0);
                List<Cell3D> neighbour = c.Neighbour;

                foreach (Cell3D nc in neighbour)
                {
                    if (nc.IsChecked == false)
                    {
                        if ((startCell.Phase > 0 && nc.Phase > 0) || //gas
                            (startCell.Phase == 0 && nc.Phase == 0))//liquid
                        {
                            nc.IsChecked = true;
                            queue.Add(nc);
                            group.Add(nc);
                        }
                    }

                }
            }

            return group;

        }

        public void UpdateFluidDistribution()
        {
            foreach (Drop3D drop in DropList)
            {
                if (drop.isSource)
                {
                    int numberOfInterface = drop.InterfaceList.Count;
                    for (int i = 0; i < numberOfInterface; i++)
                    {
                        int action = InterfaceAction(drop.InterfaceList[i]);
                        TakeAction(drop.InterfaceList[i], action);
                    }
                }
                else
                {//前进后退相匹配着进行
                    int numberOfInterface = drop.InterfaceList.Count;
                    List<int> forwardInterfaceIndexList = new List<int>();
                    List<int> backwardInterfaceIndexList = new List<int>();

                    for (int i = 0; i < numberOfInterface; i++)
                    {
                        int action = InterfaceAction(drop.InterfaceList[i]);
                        if (action == 1) forwardInterfaceIndexList.Add(i);
                        else if (action == -1) backwardInterfaceIndexList.Add(i);
                    }
                    int nf = forwardInterfaceIndexList.Count;
                    int nb = backwardInterfaceIndexList.Count;
                    int nn = Math.Min(nf, nb);

                    for (int i = 0; i < nn; i++)
                    {
                        TakeAction(drop.InterfaceList[forwardInterfaceIndexList[i]], 1);
                        TakeAction(drop.InterfaceList[backwardInterfaceIndexList[i]], -1);
                    }
                }
            }
        }

        private int InterfaceAction(Interface3D itf)
        {
            Cell3D liquidCell = itf.LiquidCell;
            Cell3D gasCell = itf.GasCell;

            double liquidPressure = DropList[liquidCell.Index].Pressure;
            double gasVolume = BubbleList[gasCell.Index].BubbleVolume;
            double gasAmount = BubbleList[gasCell.Index].BubbleGasAmount;
            //double gasPressure = BubbleList[gasCell.index].Pressure;

            int action = 0;
            if (gasVolume > 1 && liquidPressure >= gasAmount / (gasVolume - 1))
            {
                action = 1;
            }
            else if (liquidPressure < gasAmount / gasVolume)
            {
                action = -1;
                foreach (Source3D s in SourceList)
                {
                    if (s.SourceCell == liquidCell)
                    {
                        action = 0;
                    }
                }
            }
            else if (gasVolume == 1 && liquidPressure >= gasAmount * 1.5)
            {
                action = 2;//小气泡消失
            }

            return action;
        }

        private void TakeAction(Interface3D itf, int action)
        {
            Cell3D gasCell = itf.GasCell;
            Cell3D liquidCell = itf.LiquidCell;

            if (action == 1)
            {
                List<Cell3D> neighbour = gasCell.Neighbour;
                List<Cell3D> gasNeighbour = new List<Cell3D>();//gasCell的gas邻域
                foreach (Cell3D nc in neighbour)
                {
                    if (nc.Phase > 0) gasNeighbour.Add(nc);
                }
                if (gasNeighbour.Count > 0)
                {
                    double addtion = gasCell.Phase / gasNeighbour.Count;
                    foreach (Cell3D gnc in gasNeighbour)
                    {
                        gnc.Phase += addtion;
                    }
                    gasCell.Phase = 0;//update phase
                }

            }
            else if (action == -1)
            {
                liquidCell.Phase += gasCell.Phase / 2;//update phase
                gasCell.Phase -= gasCell.Phase / 2;
            }
            else if (action == 2)
            {
                gasCell.Phase = 0;//直接消失
            }
        }


        
    }
}
