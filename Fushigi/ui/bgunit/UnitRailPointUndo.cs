using Fushigi.ui.widgets;
using Fushigi.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Fushigi.ui
{
    internal class UnitRailPointAddUndo : IRevertable
    {
        public string Name { get; set; }

        public BGUnitRail Rail;

        public BGUnitRail.RailPoint Point;

        public int Index;

        public UnitRailPointAddUndo(BGUnitRail rail, BGUnitRail.RailPoint point, int index = -1)
        {
            //Undo display name
            Name = $"{IconUtil.ICON_PLUS_CIRCLE} Rail Point Add";
            //The rail to remove the point to
            Rail = rail;
            //The point to remove
            Point = point;
            Index = index;
        }

        public IRevertable Revert()
        {
            var index = Index != -1 ? Index : Rail.Points.IndexOf(Point);

            //Revert to removale
            if (Rail.Points.Contains(Point))
                Rail.Points.Remove(Point);

            //Create revert stack
            return new UnitRailPointDeleteUndo(Rail, Point, index);
        }
    }

    internal class UnitRailPointDeleteUndo : IRevertable
    {
        public string Name { get; set; }

        public BGUnitRail Rail;

        public BGUnitRail.RailPoint Point;

        public int Index;

        public UnitRailPointDeleteUndo(BGUnitRail rail, BGUnitRail.RailPoint point, int index = -1)
        {
            //Undo display name
            Name = $"{IconUtil.ICON_TRASH} Rail Point Remove";
            //The rail to add the point to
            Rail = rail;
            //The point to add
            Point = point;
            Index = index;
            //Keep original point placement
            if (rail.Points.Contains(Point) && index == -1)
                Index = rail.Points.IndexOf(Point);
        }

        public IRevertable Revert()
        {
            //Revert to removale
            if (!Rail.Points.Contains(Point))
            {
                if (Index != -1)
                    Rail.Points.Insert(Index, Point);
                else
                    Rail.Points.Add(Point);
            }

            //Create revert stack
            return new UnitRailPointAddUndo(Rail, Point);
        }
    }
}
