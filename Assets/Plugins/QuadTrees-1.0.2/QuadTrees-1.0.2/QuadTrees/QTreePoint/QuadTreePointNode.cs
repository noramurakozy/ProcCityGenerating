using System.Diagnostics;
using System.Drawing;
using QuadTrees.Common;
using QuadTrees.QTreeRect;

namespace QuadTrees.QTreePoint
{
    /// <summary>
    /// A QuadTree Object that provides fast and efficient storage of objects in a world space.
    /// </summary>
    /// <typeparam name="T">Any object implementing IQuadStorable.</typeparam>
    public class QuadTreePointNode<T> : QuadTreeNodeCommon<T, QuadTreePointNode<T>> where T : IPointQuadStorable
    {
        public QuadTreePointNode(RectangleF rect)
            : base(rect)
        {
        }

        public QuadTreePointNode(float x, float y, float width, float height)
            : base(x, y, width, height)
        {
        }

        internal QuadTreePointNode(QuadTreePointNode<T> parent, RectangleF rect)
            : base(parent, rect)
        {
        }

        protected override QuadTreePointNode<T> CreateNode(RectangleF rectangleF)
        {
            VerifyNodeAssertions(rectangleF);
            return new QuadTreePointNode<T>(this, rectangleF);
        }

        protected override bool CheckContains(RectangleF rectangleF, T data)
        {
            return rectangleF.Contains(data.Point);
        }

        public override bool ContainsObject(QuadTreeObject<T, QuadTreePointNode<T>> qto)
        {
            return CheckContains(QuadRect, qto.Data);
        }

        protected override bool CheckIntersects(RectangleF searchRect, T data)
        {
            return searchRect.Contains(data.Point);
        }

        protected override PointF GetMortonPoint(T p)
        {
            return p.Point;
        }
    }
}