﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using SharpGL;

namespace MineCad
{
    public partial class MainForm : Form
    {
        /* Настройка цвета фона OpenGLControl. */
        private const float backgroundColorRConstant = 0.3f;
        private const float backgroundColorGConstant = 0.3f;
        private const float backgroundColorBConstant = 0.4f;

        /* Координаты последнего клика мышью в рабочей зоне. */
        private int pressedMouseX = 0;
        private int pressedMouseY = 0;

        /* Режимы работы. */
        private bool isCreatingMode = false;
        private bool isEditingMode = false;    // TODO.

        /* TODO: Режимы преобразования объекта. */
        private bool isRotatingMode = false;
        private bool isMovingMode = false;
        private bool isScalingMode = false;

        /* Режимы преобразования сцены. */
        private bool isSceneRotatingMode = false;
        private bool isSceneMovingMode = false;

        /* Параметры, отвечающие за вращение сцены. */
        private float axisRotateX = 15.0f;
        private float axisRotateY = -45.0f;
        private float rotationSpeed = 1.0f;

        /* Параметры, отвечающие за перемещение сцены. */
        private float newXCoordinate = 0.0f;
        private float newYCoordinate = 0.0f;
        private float newZCoordinate = -15.0f;    // Параметр, отвечающий за расстояние от камеры до центра главной системы координат.
        private float movingSpeed = 0.035f;

        /* Параметры, отвечающие за масштаб сцены. */
        private float scale = 1.0f;
        private float scaleSpeed = 0.1f;

        /* Параметры главной системы координат. */
        private bool isCoordinateSystemVisible = true;
        private float coordinateSystemSize = 30.0f;
        private float coordinateSystemLineWidth = 3.0f;

        /* Параметры главных сеток. */
        private bool isXYGridVisible = false;
        private bool isYZGridVisible = false;
        private bool isXZGridVisible = true;
        private uint gridSizeInCells = 30;
        private float gridCellSize = 2.0f;
        private float gridLineWidth = 1.0f;

        /* В тестовом режиме: проверка создания куба. */
        private float cubeSize = 10.0f;
        private float cubeX = 0.0f;
        private float cubeY = 0.0f;
        private bool isCubeCreated = false;

        /* В тестовом режиме: параметры гизмо. */
        private bool isStartVisible = false;

        /* В тестовом режиме: отображение контуров примитивов. */
        private bool areObjectsContoursVisible = false;

        /* В тестовом режиме: создание различных примитивов. */
        private bool isPlaneVisible = false;
        private bool isCubeVisible = false;
        private bool isParallelepipedVisible = false;
        private bool isPyramidVisible = false;
        private bool isCylinderVisible = false;
        private bool isConeVisible = false;
        private bool isSphereVisible = false;

        private Tank[] tankPlatoon = { new Tank(new Point(0, 0, 0)),
                                       new Tank(new Point(0, 0, 20)), 
                                       new Tank(new Point(0, 0, -20)) };

        private Bullet[] bullets = { new Bullet(), new Bullet(), new Bullet() };
        private bool wasFire = false;

        public MainForm()
        {
            InitializeComponent();

            /* Обработка событий мыши. */
            this.MouseWheel += SceneScaling;
        }

        private void OpenGLControl1_OpenGLInitialized(object sender, EventArgs e)
        {
            /* Получение ссылки на окно OpenGL. */
            OpenGL gl = this.openGLControl.OpenGL;

            /* Установка цвета, которым будет заполнен буфер кадра. */
            gl.ClearColor(backgroundColorRConstant, backgroundColorGConstant, backgroundColorBConstant, 1.0f);
        }

        private void OpenGLControl1_OpenGLDraw(object sender, RenderEventArgs args)
        {
            OpenGL gl = this.openGLControl.OpenGL;

            /* Установка цвета, которым будет заполнен буфер кадра. */
            gl.ClearColor(backgroundColorRConstant, backgroundColorGConstant, backgroundColorBConstant, 1.0f);

            /* Очистка буферов цвета и глубины. */
            gl.Clear(OpenGL.GL_COLOR_BUFFER_BIT | OpenGL.GL_DEPTH_BUFFER_BIT);

            gl.LoadIdentity();

            /* Преобразование сцены. */
            TransformScene(gl);

            /* Отрисовка главной системы координат. */
            if (this.isCoordinateSystemVisible)
            {
                Axes.Draw(gl, new Point(), this.coordinateSystemSize,
                        this.coordinateSystemLineWidth, this.coordinateSystemLineWidth, this.coordinateSystemLineWidth,
                        System.Drawing.Color.Red, System.Drawing.Color.GreenYellow, System.Drawing.Color.DeepSkyBlue);
            }

            /* Создание гизмо в центре. */
            if (this.isStartVisible)
            {
                GLDrawHelper.DrawStartTriangle(gl);
            }

            /* Отрисовка главных сеток. */
            if (this.isXYGridVisible)
            {
                Grid.Draw(gl, 0, new Point(), this.gridCellSize, this.gridSizeInCells, this.gridSizeInCells,
                        this.gridLineWidth, System.Drawing.Color.DarkGray);
            }

            if (this.isYZGridVisible)
            {
                Grid.Draw(gl, 1, new Point(), this.gridCellSize, this.gridSizeInCells, this.gridSizeInCells,
                        this.gridLineWidth, System.Drawing.Color.DarkGray);
            }

            if (this.isXZGridVisible)
            {
                Grid.Draw(gl, 2, new Point(), this.gridCellSize, this.gridSizeInCells, this.gridSizeInCells,
                        this.gridLineWidth, System.Drawing.Color.DarkGray);
            }

            if (this.isCreatingMode)
            {
                GLDrawHelper.DrawCube3D(gl, this.cubeX, this.cubeY, 0.0f,
                        this.cubeSize, 3.0f, System.Drawing.Color.OrangeRed);
            }

            if (this.isCubeCreated)
            {
                GLDrawHelper.DrawFilledCube3D(gl, this.cubeX, this.cubeY, 0.0f,
                        this.cubeSize, System.Drawing.Color.LightSkyBlue);
            }

            DrawTestSolids(gl);

            foreach (var tank in this.tankPlatoon)
            {
                tank.Draw(gl, System.Drawing.Color.Green, System.Drawing.Color.Green, System.Drawing.Color.Green);
            }

            if (wasFire)
            {
                foreach (var bullet in this.bullets)
                {
                    bullet.Draw(gl, 0.09f, System.Drawing.Color.Red);
                    bullet.Go();
                }
            }

            foreach (var tank in this.tankPlatoon)
            {
                if ((this.coordinateSystemSize < tank.Center.X) && (tank.RotationValue.Y > -180.0f))
                {
                    Point newRotationValue = tank.RotationValue;

                    tank.Step = new Point();

                    newRotationValue.Y -= 0.5f;
                    tank.RotationValue = newRotationValue;
                }
                else if ((-this.coordinateSystemSize > tank.Center.X) && (tank.RotationValue.Y < 0.0f))
                {
                    Point newRotationValue = tank.RotationValue;

                    tank.Step = new Point();

                    newRotationValue.Y += 0.5f;
                    tank.RotationValue = newRotationValue;
                }
                else
                {
                    if (tank.RotationValue.Y <= -180.0f)
                    {
                        tank.Step = new Point(-0.15f, 0.0f, 0.0f);
                    }
                    else if (tank.RotationValue.Y >= 0.0f)
                    {
                        tank.Step = new Point(0.15f, 0.0f, 0.0f);
                    }
                }

                tank.Go();
            }

            gl.Flush();

            gl.LoadIdentity();
        }

        private void TransformScene(OpenGL gl)
        {
            /* Перемещение сцены. */
            gl.Translate(this.newXCoordinate, this.newYCoordinate, this.newZCoordinate);

            /* Вращение сцены. */
            gl.Rotate(this.axisRotateX, 1.0f, 0.0f, 0.0f);
            gl.Rotate(this.axisRotateY, 0.0f, 1.0f, 0.0f);

            /* Масштабирование сцены. */
            gl.Scale(this.scale, this.scale, this.scale);
        }

        private void DrawTestSolids(OpenGL gl)
        {
            if (this.isPlaneVisible)
            {
                GLDrawHelper.DrawPlane2D(gl, 2, 0.0f, 0.0f, -10.0f, 5.0f, System.Drawing.Color.Coral);
            }

            if (this.isCubeVisible)
            {
                GLDrawHelper.DrawFilledCube3D(gl, 0.0f, 0.0f, -20.0f, 5.0f, System.Drawing.Color.DarkGreen);
            }

            if (this.isParallelepipedVisible)
            {
                GLDrawHelper.DrawFilledBox3D(gl, 0.0f, 0.0f, -30.0f, 4.0f, 3.0f, 5.0f, System.Drawing.Color.Aqua);
            }

            if (this.isPyramidVisible)
            {
                GLDrawHelper.DrawPyramid(gl, 5.0f, 6.0f, 3.0f, System.Drawing.Color.CadetBlue, System.Drawing.Color.Gold);
            }

            if (this.isCylinderVisible)
            {
                GLDrawHelper.DrawCylinder(gl, 10.0f, 2.5f, 2.5f, 24, System.Drawing.Color.LightSkyBlue, System.Drawing.Color.Chocolate);
            }

            if (this.isConeVisible)
            {
                GLDrawHelper.DrawCylinder(gl, 10.0f, 2.5f, 0.0f, 24, System.Drawing.Color.LightSkyBlue, System.Drawing.Color.Chocolate);
            }

            if (this.isSphereVisible)
            {
                GLDrawHelper.DrawSphere(gl, 4.0f, System.Drawing.Color.Red, System.Drawing.Color.SeaShell);
            }
        }

        private void OpenGLControl1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (!this.isCreatingMode)
                {
                    this.isSceneRotatingMode = true;
                }
                else
                {
                    this.isCreatingMode = false;
                    this.isCubeCreated = true;
                }
            }
            else if (e.Button == MouseButtons.Right) 
            { 
                this.isSceneMovingMode = true; 
            }

            this.pressedMouseX = e.X;
            this.pressedMouseY = e.Y;
        }

        private void OpenGLControl1_MouseUp(object sender, MouseEventArgs e)
        {
            this.isSceneRotatingMode = false;
            this.isSceneMovingMode = false;
        }

        private void OpenGLControl1_MouseMove(object sender, MouseEventArgs e)
        {
            int deltaX = this.pressedMouseX - e.X;
            int deltaY = this.pressedMouseY - e.Y;

            if (this.isSceneMovingMode)
            {
                this.newXCoordinate -= this.movingSpeed * deltaX;
                this.newYCoordinate += this.movingSpeed * deltaY;
            }
            else if (this.isSceneRotatingMode)
            {
                this.axisRotateX -= this.rotationSpeed * deltaY;
                this.axisRotateY -= this.rotationSpeed * deltaX;
            }
            else if (this.isCreatingMode)
            {
                this.cubeSize += this.movingSpeed * deltaX;
                this.cubeSize += this.movingSpeed * deltaY;
            }

            this.pressedMouseX = e.X;
            this.pressedMouseY = e.Y;
        }

        void SceneScaling(object sender, MouseEventArgs e)
        {
            if (e.Delta != 0)
            {
                float newScale = this.scale + (e.Delta / Math.Abs(e.Delta)) * this.scaleSpeed * this.scale;
                this.scale = (newScale > 0.0f) ? newScale : this.scale;
            }
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            this.isStartVisible = !this.isStartVisible;
        }

        private void IisometryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            /* Сброс параметров, отвечающих за вращение сцены. */
            this.axisRotateX = 15.0f;
            this.axisRotateY = -45.0f;

            /* Сброс параметров, отвечающих за перемещение сцены. */
            this.newXCoordinate = 0.0f;
            this.newYCoordinate = 0.0f;
            this.newZCoordinate = -15.0f;

            /* Сброс параметров, отвечающих за масштаб сцены. */
            this.scale = 1.0f;
        }

        private void MainAxesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isCoordinateSystemVisible = !this.isCoordinateSystemVisible;
        }

        private void GridXYToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isXYGridVisible = !this.isXYGridVisible;
        }

        private void GridYZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isYZGridVisible = !this.isYZGridVisible;
        }

        private void GridXZToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isXZGridVisible = !this.isXZGridVisible;
        }

        private void ObjectsContoursToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.areObjectsContoursVisible = !this.areObjectsContoursVisible;
        }

        private void PlaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isPlaneVisible = !this.isPlaneVisible;
        }

        private void CubeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isCubeVisible = !this.isCubeVisible;
        }

        private void ParallelepipedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isParallelepipedVisible = !this.isParallelepipedVisible;
        }

        private void PyramidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isPyramidVisible = !this.isPyramidVisible;
        }

        private void CylinderToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isCylinderVisible = !this.isCylinderVisible;
        }

        private void ConeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isConeVisible = !this.isConeVisible;
        }

        private void SphereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.isSphereVisible = !this.isSphereVisible;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.isCreatingMode = true;
            this.isCubeCreated = false;
            this.pressedMouseX = 0;
            this.pressedMouseY = 0;
        }

        private void Fire_Click(object sender, EventArgs e)
        {
            wasFire = true;

            int i = 0;

            foreach (var tank in this.tankPlatoon)
            {
                this.bullets[i] = tank.Fire();
                i++;
            }
        }
    }
}
