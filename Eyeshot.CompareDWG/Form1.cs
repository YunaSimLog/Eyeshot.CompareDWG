using devDept.Eyeshot;
using devDept.Eyeshot.Control;
using devDept.Eyeshot.Entities;
using devDept.Eyeshot.Translators;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
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
            base.OnLoad(e);
        }

        private void CameraChanged(object sender, CameraMoveEventArgs e)
        {
            //throw new NotImplementedException();
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
