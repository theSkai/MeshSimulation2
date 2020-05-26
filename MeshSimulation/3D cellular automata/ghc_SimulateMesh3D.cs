using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

namespace MeshSimulation
{
    public class ghc_SimulateMesh3D : GH_Component
    {
        public ghc_SimulateMesh3D()
          : base("MeshSimulation3D", "MeshSimulation3D",
              "simulate mesh fluid distribution",
              "venous", "mesh")
        {
        }

        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, true);

            pManager.AddNumberParameter("X_extent", "X_extent", "X_extent", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Y_extent", "Y_extent", "Y_extent", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Z_extent", "Z_extent", "Z_extent", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Step", "Step", "Step", GH_ParamAccess.item, 0);

            pManager.AddNumberParameter("X_num", "X_num", "X_num", GH_ParamAccess.item);
            pManager.AddNumberParameter("Y_num", "Y_num", "Y_num", GH_ParamAccess.item);
            pManager.AddNumberParameter("Z_num", "Z_num", "Z_num", GH_ParamAccess.item);
            pManager.AddNumberParameter("EdgeLength", "EdgeLength", "EdgeLength", GH_ParamAccess.item);

            pManager.AddPointParameter("LiquidSourcePointList", "LiquidSourcePoint", "LiquidSourcePoint", GH_ParamAccess.list);
            pManager.AddNumberParameter("LiquidSourcePressureList", "LiquidSourcePressure", "LiquidSourcePressure", GH_ParamAccess.list);
        }

        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output", "Output", "Output", GH_ParamAccess.list);
            pManager.AddGenericParameter("DisplayMesh", "DisplayMesh", "DisplayMesh", GH_ParamAccess.list);
        }

        public Canvas3D canvas = new Canvas3D();
        public Canvas3D canvas1 = new Canvas3D();
        public Canvas3D canvas2 = new Canvas3D();
        bool isInitialized = false;

        List<GH_Mesh> displayMesh = new List<GH_Mesh>();
        double SumGasAmount = 0;
        List<string> output = new List<string>();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool reset = true;
            DA.GetData("Reset", ref reset);

            if (reset)
            {
                isInitialized = true;
                canvas = new Canvas3D();

                double x_extent = 0;
                double y_extent = 0;
                double z_extent = 0;
                double step = 0;

                double x_num = 0;
                double y_num = 0;
                double z_num = 0;
                double edgeLength = 0;

                List<Point3d> liquidSource = new List<Point3d>();
                List<double> liquidSourcePressure = new List<double>();

                DA.GetData("X_extent", ref x_extent);
                DA.GetData("Y_extent", ref y_extent);
                DA.GetData("Z_extent", ref z_extent);
                DA.GetData("Step", ref step);

                DA.GetData("X_num", ref x_num);
                DA.GetData("Y_num", ref y_num);
                DA.GetData("Z_num", ref z_num);
                DA.GetData("EdgeLength", ref edgeLength);

                DA.GetDataList("LiquidSourcePointList", liquidSource);
                DA.GetDataList("LiquidSourcePressureList", liquidSourcePressure);

                canvas.CreateEmptyMesh((int)x_extent, (int)y_extent, (int)z_extent, step);
                canvas.InitializeMesh((int)edgeLength, (int)x_num, (int)y_num, (int)z_num);
                canvas.LoadLiquidSource(liquidSource, liquidSourcePressure);
                canvas1.InitializeVenousCellListFrom(canvas.VenousCellList);
                canvas2.InitializeVenousCellListFrom(canvas.VenousCellList);

                SumGasAmount = canvas.SumGasAmount;
                output.Clear();
                output.Add(SumGasAmount.ToString());
            }
            
            else if (isInitialized)
            {
                List<double> liquidSourcePressure = new List<double>();
                DA.GetDataList("LiquidSourcePressureList", liquidSourcePressure);

                int updateTimes = 0;
                while (updateTimes < 100)
                {
                    canvas.UpdateLiquidSourcePressure(liquidSourcePressure);
                    canvas.ReadMesh();
                    canvas.UpdateFluidDistribution();
                    SumGasAmount = canvas.SumGasAmount;

                    if (updateTimes >= 2)
                    {
                        if (canvas2.IsSameVenousCellListAs(canvas.VenousCellList))
                        {
                            break;
                        }
                    }

                    canvas2.DuplicateVenousCellListFrom(canvas1.VenousCellList);
                    canvas1.DuplicateVenousCellListFrom(canvas.VenousCellList);

                    updateTimes++;
                }
                output.Clear();
                output.Add("update times: " + updateTimes.ToString());
                output.Add("gas amount: " + SumGasAmount.ToString());
            }

            displayMesh = canvas.DisplayMeshColored();

            DA.SetDataList("DisplayMesh", displayMesh);
            DA.SetDataList("Output", output);

        }

        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return null;
            }
        }

        public override Guid ComponentGuid
        {
            get { return new Guid("0619c7e2-a76e-4a01-83d6-13e0b1a46b11"); }
        }
    }
}