using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MeshSimulation
{
    public class Cell3D
    {
        public double Volume;//0->wall, 1->channel
        public double Phase;//-1-> fluidsource, 0->fluid, >0->gas
        public int Index;//bubble/drop index
        public int X_coor;
        public int Y_coor;
        public int Z_coor;
        public bool IsChecked;
        
        public List<Cell3D> Neighbour;

        public Cell3D(int x, int y, int z, double volume = 0, int gasAmount = 0)
        {
            X_coor = x;
            Y_coor = y;
            Z_coor = z;
            Volume = volume;
            Phase = gasAmount;
            Index = -1;
            IsChecked = false;
            Neighbour = new List<Cell3D>();
        }
    }
}
