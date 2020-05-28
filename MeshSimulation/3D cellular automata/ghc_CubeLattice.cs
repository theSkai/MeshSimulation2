using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace MeshSimulation._3D_cellular_automata
{
    public class ghc_CubeLattice : GH_Component
    {
        /// <summary>
        /// Initializes a new instance of the ghc_CubeLattice class.
        /// </summary>
        public ghc_CubeLattice()
          : base("CubeLattice", "CubeLattice",
              "CubeLattice",
              "venous", "mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddNumberParameter("EdgeLength", "EdgeLength", "EdgeLength", GH_ParamAccess.item);
            pManager.AddNumberParameter("X_num", "X_num", "X_num", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y_num", "Y_num", "Y_num", GH_ParamAccess.item);
            pManager.AddNumberParameter("Z_num", "Z_num", "Z_num", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("VesselCellList", "VesselCellList", "VesselCellList", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {
            double x_num = 0;
            double y_num = 0;
            double z_num = 0;
            double edgeLength = 0;
            DA.GetData("X_num", ref x_num);
            DA.GetData("Y_num", ref y_num);
            DA.GetData("Z_num", ref z_num);
            DA.GetData("EdgeLength", ref edgeLength);
            int X_num = (int)x_num;
            int Y_num = (int)y_num;
            int Z_num = (int)z_num;
            int EdgeLength = (int)edgeLength;
            if (X_num == 0 || Y_num == 0 || Z_num == 0 || EdgeLength == 0) return;

            int X_extent = X_num * EdgeLength + 1;
            int Y_extent = Y_num * EdgeLength + 1;
            int Z_extent = Z_num * EdgeLength + 1;

            CreateEmptyMesh(X_extent, Y_extent, Z_extent);
            InitializeMesh(X_extent, Y_extent, Z_extent, EdgeLength);

            DA.SetDataList("VesselCellList", VesselCellList);

        }

        public List<List<List<Cell3D>>> CellMesh = new List<List<List<Cell3D>>>();//x,y,z 
        public List<Cell3D> VesselCellList = new List<Cell3D>();

        public void CreateEmptyMesh(int X_extent, int Y_extent, int Z_extent)
        {
            CellMesh.Clear();//开新空间
            for (int i = 0; i < X_extent; i++)
            {
                List<List<Cell3D>> columnYZ = new List<List<Cell3D>>();
                for (int j = 0; j < Y_extent; j++)
                {
                    List<Cell3D> columnZ = new List<Cell3D>();
                    List<bool> columnOfIsDiscoveredZ = new List<bool>();
                    for (int k = 0; k < Z_extent; k++)
                    {
                        Cell3D c = new Cell3D(i, j, k);
                        columnZ.Add(c);
                        columnOfIsDiscoveredZ.Add(false);
                    }
                    columnYZ.Add(columnZ);
                }
                CellMesh.Add(columnYZ);
            }
        }

        public void InitializeMesh(int X_extent, int Y_extent, int Z_extent, int EdgeLength)
        {
            VesselCellList.Clear();//开新空间

            for (int i = 0; i < X_extent; i++)
            {
                for (int j = 0; j < Y_extent; j++)
                {
                    for (int k = 0; k < Z_extent; k++)
                    {
                        if ((i % EdgeLength == 0 && j % EdgeLength == 0) ||
                           (i % EdgeLength == 0 && k % EdgeLength == 0) ||
                           (j % EdgeLength == 0 && k % EdgeLength == 0))
                        {
                            //定义空腔
                            CellMesh[i][j][k].Volume = 1;
                            CellMesh[i][j][k].Phase = 1;

                            //计算邻域
                            if (i % EdgeLength == 0 && j % EdgeLength == 0 && k % EdgeLength == 0)
                            {//顶点
                                if (i + 1 < X_extent) CellMesh[i][j][k].Neighbour.Add(CellMesh[i + 1][j][k]);
                                if (i - 1 >= 0) CellMesh[i][j][k].Neighbour.Add(CellMesh[i - 1][j][k]);
                                if (j + 1 < Y_extent) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j + 1][k]);
                                if (j - 1 >= 0) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j - 1][k]);
                                if (k + 1 < Z_extent) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j][k + 1]);
                                if (k - 1 >= 0) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j][k - 1]);
                            }
                            else if (i % EdgeLength != 0)
                            {
                                if (i + 1 < X_extent) CellMesh[i][j][k].Neighbour.Add(CellMesh[i + 1][j][k]);
                                if (i - 1 >= 0) CellMesh[i][j][k].Neighbour.Add(CellMesh[i - 1][j][k]);
                            }
                            else if (j % EdgeLength != 0)
                            {
                                if (j + 1 < Y_extent) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j + 1][k]);
                                if (j - 1 >= 0) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j - 1][k]);
                            }
                            else if (k % EdgeLength != 0)
                            {
                                if (k + 1 < Z_extent) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j][k + 1]);
                                if (k - 1 >= 0) CellMesh[i][j][k].Neighbour.Add(CellMesh[i][j][k - 1]);
                            }

                            VesselCellList.Add(CellMesh[i][j][k]);//按照坐标序加入
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("a2df06cf-3a82-41a7-b217-70068aa2287f"); }
        }
    }
}