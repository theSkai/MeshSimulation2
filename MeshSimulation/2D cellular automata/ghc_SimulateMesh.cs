using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Rhino.Geometry;

// In order to load the result of this wizard, you will also need to
// add the output bin/ folder of this project to the list of loaded
// folder in Grasshopper.
// You can use the _GrasshopperDeveloperSettings Rhino command for that.

namespace MeshSimulation
{
    public class ghc_SimulateMesh : GH_Component
    {
        /// <summary>
        /// Each implementation of GH_Component must provide a public 
        /// constructor without any arguments.
        /// Category represents the Tab in which the component will appear, 
        /// Subcategory the panel. If you use non-existing tab or panel names, 
        /// new tabs/panels will automatically be created.
        /// </summary>
        public ghc_SimulateMesh()
          : base("MeshSimulation", "MeshSimulation",
              "simulate mesh fluid distribution",
              "venous", "mesh")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Reset", "Reset", "Reset", GH_ParamAccess.item, true);
            pManager.AddNumberParameter("Height", "Height", "Height", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Width", "Width", "Width", GH_ParamAccess.item, 0);
            pManager.AddNumberParameter("Step", "Step", "Step", GH_ParamAccess.item, 0);
            pManager.AddIntegerParameter("VolumeMesh", "VolumeMesh", "VolumeMesh", GH_ParamAccess.tree);
            pManager.AddPointParameter("LiquidSourcePointList", "LiquidSourcePoint", "LiquidSourcePoint", GH_ParamAccess.list);
            pManager.AddNumberParameter("LiquidSourcePressureList", "LiquidSourcePressure", "LiquidSourcePressure", GH_ParamAccess.list);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddGenericParameter("Output", "Output", "Output", GH_ParamAccess.list);
            pManager.AddGenericParameter("DisplayMesh", "DisplayMesh", "DisplayMesh", GH_ParamAccess.list);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object can be used to retrieve data from input parameters and 
        /// to store data in output parameters.</param>
        /// 

        public Canvas canvas = new Canvas();
        public Canvas lastCanvas1 = new Canvas();
        public Canvas lastCanvas2 = new Canvas();
        bool isInitialized = false;

        List<GH_Mesh> displayMesh = new List<GH_Mesh>();
        double SumGasAmount = 0;
        List<string> output = new List<string>();

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            bool reset = true;
            DA.GetData("Reset", ref reset);
            //List<List<int>> displayMesh = new List<List<int>>();
            //List<Color> displayMesh = new List<Color>();
            
            if (reset)
            {
                isInitialized = true;
                canvas = new Canvas();

                double width = 0;
                double height = 0;
                double step = 0;
                GH_Structure<GH_Integer> volumeMesh = new GH_Structure<GH_Integer>();
                List<Point3d> liquidSource = new List<Point3d>();
                List<double> liquidSourcePressure = new List<double>();

                DA.GetData("Height", ref height);
                DA.GetData("Width", ref width);
                DA.GetData("Step", ref step);
                DA.GetDataTree("VolumeMesh", out volumeMesh);
                DA.GetDataList("LiquidSourcePointList", liquidSource);
                DA.GetDataList("LiquidSourcePressureList", liquidSourcePressure);

                canvas.createEmptyMesh((int)width, (int)height, step);
                canvas.initializeMesh(volumeMesh);
                canvas.loadLiquidSource(liquidSource, liquidSourcePressure);

                lastCanvas1.createEmptyMesh((int)width, (int)height, step);
                lastCanvas2.createEmptyMesh((int)width, (int)height, step);

                SumGasAmount = canvas.SumGasAmount;

            }
            else if(isInitialized)
            {
                List<double> liquidSourcePressure = new List<double>();
                DA.GetDataList("LiquidSourcePressureList", liquidSourcePressure);

                int updateTimes = 0;

                while(updateTimes < 100)
                {
                    canvas.updateLiquidSourcePressure(liquidSourcePressure);
                    canvas.readMesh();
                    canvas.updateFluidDistribution();
                    SumGasAmount = canvas.SumGasAmount;

                    if (updateTimes >= 2)
                    {
                        if (lastCanvas2.isSameCellMeshAs(canvas.CellMesh))
                        {
                            break;
                        }
                    }

                    lastCanvas2.duplicateCellMeshFrom(lastCanvas1.CellMesh);
                    lastCanvas1.duplicateCellMeshFrom(canvas.CellMesh);

                    updateTimes++;
                }
                output.Clear();
                output.Add("update times: " + updateTimes.ToString());
                output.Add("gas amount: " + SumGasAmount.ToString());
                DA.SetDataList("Output", output);
            }
            displayMesh = canvas.displayMeshColored();
            DA.SetDataList("DisplayMesh", displayMesh);

        }

        /// <summary>
        /// Provides an Icon for every component that will be visible in the User Interface.
        /// Icons need to be 24x24 pixels.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                // You can add image files to your project resources and access them like this:
                //return Resources.IconForThisComponent;
                return null;
            }
        }

        /// <summary>
        /// Each component must have a unique Guid to identify it. 
        /// It is vital this Guid doesn't change otherwise old ghx files 
        /// that use the old ID will partially fail during loading.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("553faddb-a8dc-4db6-9925-c44b45b50886"); }
        }
    }
}
