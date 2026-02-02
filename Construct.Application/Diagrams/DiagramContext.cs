using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Construct.Application.Diagrams
{

    // --- Enums voor wat we willen zien ---
    
    // -- Wat voor view (geometry, geometry + loadcase, diagram)
    public enum ViewType
    {
        Geometry,
        GeometryWithLoadCase,
        Diagram
    }
    // -- Wat voor diagram (shear, bending, deflection)
    public enum DiagramType
    {
        ShearForceVz,
        BendingMomentMy,
        DeflectionW
    }
    // -- Wat is de bron van het diagram (loadcase, loadcombination, loadcombinationtype)
    public enum DiagramSource
    {
        LoadCase,
        LoadCombination,
        LoadCombinationType
    }

    public sealed class OptionsContext
    {
        public ViewType ViewType { get; init; }

        // geometry + loadcase (figuren)
        public Eurocode.Belastingen.BelastingGeval? LoadCase { get; init; }

        // diagram
        public DiagramContext? Diagram { get; init; }

        public static OptionsContext GetFiguur(Eurocode.Belastingen.BelastingGeval? loadCase)
        {
            var viewType = ViewType.GeometryWithLoadCase;
            if (loadCase == null)
            {
                viewType = ViewType.Geometry;
            }

            OptionsContext options = new()
            {
                LoadCase = loadCase,
                ViewType = viewType,
            };
            return options; 
        }

        public static OptionsContext GetDiagram(Eurocode.Belastingen.BelastingGeval? loadCase, DiagramType diagramType)
        {
            OptionsContext options = new()
            {
                ViewType = ViewType.Diagram,
                Diagram = new DiagramContext
                {
                    DiagramType = diagramType,
                    Source = DiagramSource.LoadCase,
                    LoadCase = loadCase,
                }
            };
            return options;
        }

        public static OptionsContext GetDiagram(Eurocode.Belastingen.BelastingCombinatie? loadCombination, DiagramType diagramType)
        {
            OptionsContext options = new()
            {
                ViewType = ViewType.Diagram,
                Diagram = new DiagramContext
                {
                    DiagramType = diagramType,
                    Source = DiagramSource.LoadCombination,
                    LoadCombination = loadCombination,
                }
            };
            return options;
        }

        public static OptionsContext GetDiagram(Eurocode.Belastingen.BelastingCombinatieTypeEnum? combiType, DiagramType diagramType)
        {
            OptionsContext options = new()
            {
                ViewType = ViewType.Diagram,
                Diagram = new DiagramContext
                {
                    DiagramType = diagramType,
                    Source = DiagramSource.LoadCombinationType,
                    LoadCombinationType = combiType,
                }
            };
            return options;
        }


    }

    public sealed class DiagramContext
    {
        public DiagramType DiagramType { get; init; } // welk diagram
        public DiagramSource Source { get; init; } // van waar komt het diagram (de data)

        public Eurocode.Belastingen.BelastingGeval? LoadCase { get; init; } // voor diagram van 1 loadcase
        public Eurocode.Belastingen.BelastingCombinatie? LoadCombination { get; init; } // voor diagram van 1 loadcombination
        public Eurocode.Belastingen.BelastingCombinatieTypeEnum? LoadCombinationType { get; init; } // voor diagram van alle loadcombinations van een of meerdere type (gebruikt flags)
        
        // voor terugkoppeling
        public double Scale { get; set; }
        public double ScaleX { get; set; }
    }
}
