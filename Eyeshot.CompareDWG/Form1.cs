using devDept.Eyeshot;
using devDept.Eyeshot.Control;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Eyeshot.CompareDWG
{
    public partial class Form1 : Form
    {
        static readonly Color NOT_MODIFIED_COLOR = Color.White;

        public Form1()
        {
            InitializeComponent();

            design1.ActiveViewport.Rotate.Enabled = false;
            design2.ActiveViewport.Rotate.Enabled = false;

            design1.ActiveViewport.ViewCubeIcon.Visible = false;
            design2.ActiveViewport.ViewCubeIcon.Visible = false;

            #region Camera Sync

            design1.AnimateCamera = false;
            design2.AnimateCamera = false;

            design1.CameraChangedFrequency = 200;
            design2.CameraChangedFrequency = 200;

            design1.CameraChanged += CameraChanged;
            design2.CameraChanged += CameraChanged;

            #endregion
        }

        protected override void OnLoad(EventArgs e)
        {
            OpenFile(design1, lbBefore, @"../../../Sample\app8.dwg");
            OpenFile(design2, lbAfter, @"../../../Sample\app8mod.dwg");

            CompareAndMark(design1.Entities, design2.Entities);

            base.OnLoad(e);
        }

        private void CameraChanged(object sender, CameraMoveEventArgs e)
        {
            if (sender == design1)
                SysnCamera(design1, design2);
            else
                SysnCamera(design2, design1);
        }

        private void CompareAndMark(EntityList entities1, EntityList entities2)
        {
            bool[] equalEntitiesInV2 = new bool[entities2.Count];

            for (int i = 0; i < entities1.Count; i++)
            {
                Entity entityVp1 = entities1[i];
                bool foundEqual = false;
                for (int j = 0; j < entities2.Count; j++)
                {
                    Entity entityVp2 = entities2[j];

                    if (!equalEntitiesInV2[j] && entityVp1.GetType() == entityVp2.GetType() && CompareIfEqual(entityVp1, entityVp2))
                    {
                        equalEntitiesInV2[j] = true;
                        foundEqual = true;
                        break;
                    }
                }

                if (!foundEqual)
                {
                    entities1[i].Color = Color.Yellow;
                    entities1[i].ColorMethod = colorMethodType.byEntity;
                }
            }

            for (int j = 0; j < entities2.Count; j++)
            {
                if (!equalEntitiesInV2[j])
                {
                    entities2[j].Color = Color.Yellow;
                    entities2[j].ColorMethod = colorMethodType.byEntity;
                }
            }
        }

        private bool CompareIfEqual(Entity entityVp1, Entity entityVp2)
        {
            // 요소 속성이 같은지 체크
            bool areEqualAttributes = AreEqualAttributes(entityVp1, entityVp2);

            // 요소 세부 타입에 따라 같은지 비교 체크
            bool areEqual = AreEqual(entityVp1, entityVp2);

            return areEqualAttributes && areEqual;
        }

        private bool AreEqual(Entity entityVp1, Entity entityVp2)
        {
            // 요소가 CompositeCurve형일 경우
            if (entityVp1 is CompositeCurve)
            {
                CompositeCurve cc1 = (CompositeCurve)entityVp1;
                CompositeCurve cc2 = (CompositeCurve)entityVp2;

                if (cc1.CurveList.Count != cc2.CurveList.Count)
                    return false;

                int equalCurveInListCount = 0;
                foreach (Entity entC1 in cc1.CurveList)
                {
                    foreach (Entity entC2 in cc2.CurveList)
                    {
                        if (entC1.GetType() != entC2.GetType())
                            continue;

                        if (CompareIfEqual(entityVp1, entC2))
                        {
                            equalCurveInListCount++;
                            break;
                        }
                    }
                }

                if (equalCurveInListCount == cc1.CurveList.Count)
                    return true;
            }

            // 요소가 LinearPath형일 경우
            else if (entityVp1 is LinearPath)
            {
                LinearPath lp1 = (LinearPath)entityVp1;
                LinearPath lp2 = (LinearPath)entityVp2;

                if (lp1.Vertices.Length != lp2.Vertices.Length)
                    return false;

                for (int i = 0; i < lp1.Vertices.Length; i++)
                {
                    if (lp1.Vertices[i] != lp2.Vertices[i])
                        return false;
                }
                return true;
            }

            else if (entityVp1 is PlanarEntity)
            {
                PlanarEntity pe1 = (PlanarEntity)entityVp1;
                PlanarEntity pe2 = (PlanarEntity)entityVp2;

                if (pe1.Plane.AxisZ != pe2.Plane.AxisZ || pe1.Plane.AxisX != pe2.Plane.AxisX)
                    return false;

                if (entityVp1 is Arc)
                {
                    Arc arc1 = (Arc)entityVp1;
                    Arc arc2 = (Arc)entityVp2;

                    if (
                        arc1.Center == arc2.Center &&
                        arc1.Radius == arc2.Radius &&
                        arc1.Domain.Min == arc2.Domain.Min &&
                        arc1.Domain.Max == arc2.Domain.Max
                        )
                        return true;
                }
                else if (entityVp1 is Circle)
                {
                    Circle cc1 = (Circle)entityVp1;
                    Circle cc2 = (Circle)entityVp2;

                    if (
                        cc1.Center == cc2.Center &&
                        cc1.Radius == cc2.Radius &&
                        cc1.Domain.Min == cc2.Domain.Min &&
                        cc1.Domain.Max == cc2.Domain.Max
                        )
                        return true;
                }
                else if (entityVp1 is EllipticalArc)
                {
                    EllipticalArc earc1 = (EllipticalArc)entityVp1;
                    EllipticalArc earc2 = (EllipticalArc)entityVp2;

                    if (
                        earc1.Center == earc2.Center &&
                        earc1.RadiusX == earc2.RadiusX &&
                        earc1.RadiusY == earc2.RadiusY &&
                        earc1.Domain.Low == earc2.Domain.Low &&
                        earc1.Domain.High == earc2.Domain.High
                        )
                        return true;
                }
                else if (entityVp1 is Ellipse)
                {
                    Ellipse el1 = (EllipticalArc)entityVp1;
                    Ellipse el2 = (EllipticalArc)entityVp2;

                    if (
                        el1.Center == el2.Center &&
                        el1.RadiusX == el2.RadiusX &&
                        el1.RadiusY == el2.RadiusY
                        )
                        return true;
                }
                else if (entityVp1 is Text)
                {
                    if (entityVp1 is Dimension)
                    {
                        Dimension dim1 = (Dimension)entityVp1;
                        Dimension dim2 = (Dimension)entityVp2;

                        if (
                            dim1.InsertionPoint != dim2.InsertionPoint ||
                            dim1.DimLinePosition != dim2.DimLinePosition
                            )
                            return false;

                        if (entityVp1 is AngularDim)
                        {
                            AngularDim ad1 = (AngularDim)entityVp1;
                            AngularDim ad2 = (AngularDim)entityVp2;

                            if (
                                ad1.ExtLine1 == ad2.ExtLine1 &&
                                ad1.ExtLine2 == ad2.ExtLine2 &&
                                ad1.StartAngle == ad2.StartAngle &&
                                ad1.EndAngle == ad2.EndAngle &&
                                ad1.EndAngle == ad2.EndAngle
                                )
                                return true;
                        }
                        else if (entityVp1 is LinearDim)
                        {
                            LinearDim ld1 = (LinearDim)entityVp1;
                            LinearDim ld2 = (LinearDim)entityVp2;

                            if (
                                ld1.ExtLine1 == ld2.ExtLine1 &&
                                ld1.ExtLine2 == ld2.ExtLine2
                                )
                                return true;
                        }
                        else if (entityVp1 is DiametricDim)
                        {
                            DiametricDim dd1 = (DiametricDim)entityVp1;
                            DiametricDim dd2 = (DiametricDim)entityVp2;

                            if (
                                dd1.Distance == dd2.Distance &&
                                dd1.Radius == dd2.Radius &&
                                dd1.CenterMarkSize == dd2.CenterMarkSize
                                )
                                return true;
                        }
                        else if (entityVp1 is RadialDim)
                        {
                            RadialDim rd1 = (RadialDim)entityVp1;
                            RadialDim rd2 = (RadialDim)entityVp2;

                            if (
                                rd1.Radius == rd2.Radius &&
                                rd1.CenterMarkSize == rd2.CenterMarkSize
                                )
                                return true;
                        }
                        else if (entityVp1 is OrdinateDim)
                        {
                            OrdinateDim od1 = (OrdinateDim)entityVp1;
                            OrdinateDim od2 = (OrdinateDim)entityVp2;

                            if (
                                od1.DefiningPoint == od2.DefiningPoint &&
                                od1.Origin == od2.Origin &&
                                od1.LeaderEndPoint == od2.LeaderEndPoint
                                )
                                return true;
                        }
                        else
                        {
                            Debug.Print($"정의 되지 않은 치수 유형: {entityVp1.GetType()}");
                            return true;
                        }
                    }
                    else if (entityVp1 is devDept.Eyeshot.Entities.Attribute)
                    {
                        devDept.Eyeshot.Entities.Attribute att1 = (devDept.Eyeshot.Entities.Attribute)entityVp1;
                        devDept.Eyeshot.Entities.Attribute att2 = (devDept.Eyeshot.Entities.Attribute)entityVp2;

                        if (
                            att1.Value == att2.Value &&
                            att1.InsertionPoint == att2.InsertionPoint
                            )
                            return true;
                    }
                    else
                    {
                        Text tx1 = (Text)entityVp1;
                        Text tx2 = (Text)entityVp2;

                        if (
                            tx1.InsertionPoint == tx2.InsertionPoint &&
                            tx1.TextString == tx2.TextString &&
                            tx1.StyleName == tx2.StyleName &&
                            tx1.WidthFactor == tx2.WidthFactor &&
                            tx1.Height == tx2.Height
                            )
                            return true;
                    }
                }
                else
                {
                    Debug.Print($"정의 되지 않은 PlanarEntity 요소 유형: {entityVp1.GetType()}");
                    return true;
                }
            }

            else if (entityVp1 is Line)
            {
                Line line1 = (Line)entityVp1;
                Line line2 = (Line)entityVp2;

                if (
                    line1.StartPoint == line2.StartPoint &&
                    line1.EndPoint == line2.EndPoint
                    )
                    return true;
            }

            else if (entityVp1 is devDept.Eyeshot.Entities.Point)
            {
                devDept.Eyeshot.Entities.Point p1 = (devDept.Eyeshot.Entities.Point)entityVp1;
                devDept.Eyeshot.Entities.Point p2 = (devDept.Eyeshot.Entities.Point)entityVp2;

                if (p1.Position == p2.Position)
                    return true;
            }
            else if (entityVp1 is Curve)
            {
                Curve cu1= (Curve)entityVp1;
                Curve cu2= (Curve)entityVp2;

                if (
                    cu1.ControlPoints.Length != cu2.ControlPoints.Length ||
                    cu1.KnotVector.Length != cu2.KnotVector.Length ||
                    cu1.Degree != cu2.Degree
                    )
                    return false;

                for(int k=0;k<cu1.ControlPoints.Length;k++)
                {
                    if (cu1.ControlPoints[k] != cu2.ControlPoints[k])
                        return false;
                }

                for (int k = 0; k < cu1.KnotVector.Length; k++)
                {
                    if (cu1.KnotVector[k] != cu2.KnotVector[k])
                        return false;
                }

                return true;
            }
            else
            {
                Debug.Print($"정의 되지 않은 요소 유형: {entityVp1.GetType()}");
                return true;
            }

            return false;
        }

        private bool AreEqualAttributes(Entity entityVp1, Entity entityVp2)
        {
            return
                entityVp1.LayerName == entityVp2.LayerName &&
                entityVp1.GroupIndex == entityVp2.GroupIndex &&
                entityVp1.ColorMethod == entityVp2.ColorMethod &&
                entityVp1.Color == entityVp2.Color &&
                entityVp1.LineWeight == entityVp2.LineWeight &&
                entityVp1.LineWeightMethod == entityVp2.LineWeightMethod &&
                entityVp1.LineTypeName == entityVp2.LineTypeName &&
                entityVp1.LineTypeMethod == entityVp2.LineTypeMethod &&
                entityVp1.LineTypeScale == entityVp2.LineTypeScale &&
                entityVp1.MaterialName == entityVp2.MaterialName;
        }

        private void SysnCamera(Design designMovedCamera, Design designCameraToMove)
        {
            Camera savedCamera;
            designMovedCamera.SaveView(out savedCamera);

            designCameraToMove.RestoreView(savedCamera);
            designCameraToMove.AdjustNearAndFarPlanes(); // 카메라 근거리 원거리 조정
            designCameraToMove.Invalidate();
        }

        private void OpenFile(Design design, Label lable, string filePath = null)
        {
            if (filePath == null)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                DialogResult dialogResult = openFileDialog.ShowDialog(this);
                if (dialogResult != DialogResult.OK)
                    return;
                filePath = openFileDialog.FileName;
            }

            ReadFileAsync readFileAsync = GetReader(filePath);
            if (readFileAsync == null)
                return;

            btnBefore.Enabled = btnAfter.Enabled = false;
            lable.Text = "로딩중...";
            lable.Refresh();

            readFileAsync.DoWork();

            design.Clear();

            // 디자이너에 불러온 파일 요소 추가
            readFileAsync.AddTo(design);

            Entity[] toAdd = design.Entities.Explode();

            design.Entities.AddRange(toAdd, NOT_MODIFIED_COLOR);

            ColorEntities(design == design1 ? design2.Entities : design1.Entities);

            btnBefore.Enabled = btnAfter.Enabled = true;

            lable.Text = readFileAsync.FileName;

            design.SetView(viewType.Top);

            design.ZoomFit();

            design.Invalidate();
        }

        private void ColorEntities(EntityList entityList)
        {
            foreach (Entity entity in entityList)
            {
                entity.Color = NOT_MODIFIED_COLOR;
                entity.ColorMethod = colorMethodType.byEntity;
            }
        }

        private ReadFileAsync GetReader(string filePath)
        {
            string extension = Path.GetExtension(filePath);

            if (extension == null)
                return null;

            extension = extension.ToLower();

            switch (extension)
            {
                case ".dwg":
                    ReadDWG rd = new ReadDWG(filePath);
                    return rd;
                case ".dxf":
                    ReadDXF rdx = new ReadDXF(filePath);
                    return rdx;
            }
            return null;
        }
    }
}
