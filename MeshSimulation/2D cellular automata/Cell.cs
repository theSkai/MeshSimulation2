using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace MeshSimulation
{
    public class Cell
    {
        public int Volume;//0->wall, 1->channel
        public double Phase;//-1-> fluidsource, 0->fluid, >0->gas
        public int index;//bubble/drop index
        public int X_cell;
        public int Y_cell;

        public Cell(int x, int y, int volume = 0, int gasAmount = 0)
        {
            X_cell = x;
            Y_cell = y;
            Volume = volume;
            Phase = gasAmount;
            index = -1;
        }
    }
}