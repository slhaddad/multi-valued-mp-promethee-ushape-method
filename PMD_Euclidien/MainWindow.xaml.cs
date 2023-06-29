using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PMD_Euclidien
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<Bitmap> imagesBmp = new List<Bitmap>();
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Multiselect = true;
            openFile.DefaultExt = "png";
            openFile.Filter = "PNG (*.png)|*.png|JPEG (*.jpg;*jpeg)|*.jpg;*.jpeg|BMP (*.bmp)|*.bmp|TIFF (*.tiff)|*.tiff";
            openFile.ShowDialog();
            if (openFile.FileNames.Length > 0)
            {
                foreach (string filename in openFile.FileNames)
                {
                    imagesBmp.Add(new Bitmap(filename));
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            int max = int.Parse(textBox.Text);
            int itStabilite = int.Parse(textBox2.Text);
            Double prStabilite = Double.Parse(textBox3.Text) / 100;
            string[] poidsStr = textBox4.Text.Split(';');
            Double[] poids = Array.ConvertAll(poidsStr, poidStr => Double.Parse(poidStr));
            List<int[,]> imagesMat = bmpToMat(imagesBmp);
            for (int i = 1; i <= max; i++)
            {
                List<int[,]> imagesErodeInit = new List<int[,]>();
                List<int[,]> imagesDilateInit = new List<int[,]>();
                List<int[,]> imagesOuvertesStandards = new List<int[,]>();
                List<int[,]> imagesFermeesStandards = new List<int[,]>();
                ErosionDilatationInit(imagesMat, poids, ref imagesErodeInit, ref imagesDilateInit, i);
                OuvertureFermetureStandard(imagesErodeInit, imagesDilateInit, poids, ref imagesOuvertesStandards, ref imagesFermeesStandards, i);

                for (int k = 0; k < imagesBmp.Count; k++)
                {
                    Bitmap sortieErod = new Bitmap(imagesBmp[k].Width, imagesBmp[k].Height);
                    Bitmap sortieDilat = new Bitmap(imagesBmp[k].Width, imagesBmp[k].Height);
                    Bitmap sortieOuverteStandard = new Bitmap(imagesBmp[k].Width, imagesBmp[k].Height);
                    Bitmap sortieFermeeStandard = new Bitmap(imagesBmp[k].Width, imagesBmp[k].Height);
                    for (int x = 0; x < imagesBmp[k].Width; x++)
                    {
                        for (int y = 0; y < imagesBmp[k].Height; y++)
                        {
                            sortieErod.SetPixel(x, y, Color.FromArgb(imagesErodeInit[k][x, y], imagesErodeInit[k][x, y], imagesErodeInit[k][x, y]));
                            sortieDilat.SetPixel(x, y, Color.FromArgb(imagesDilateInit[k][x, y], imagesDilateInit[k][x, y], imagesDilateInit[k][x, y]));
                            sortieOuverteStandard.SetPixel(x, y, Color.FromArgb(imagesOuvertesStandards[k][x, y], imagesOuvertesStandards[k][x, y], imagesOuvertesStandards[k][x, y]));
                            sortieFermeeStandard.SetPixel(x, y, Color.FromArgb(imagesFermeesStandards[k][x, y], imagesFermeesStandards[k][x, y], imagesFermeesStandards[k][x, y]));
                        }
                    }
                    sortieErod.Save(".\\IMAGES\\Erod B_" + k + " ES_" + i + ".tiff");
                    sortieDilat.Save(".\\IMAGES\\Dilat B_" + k + " ES_" + i + ".tiff");
                    sortieOuverteStandard.Save(".\\IMAGES\\OuvertureStandard B_" + k + " ES_" + i + ".tiff");
                    sortieFermeeStandard.Save(".\\IMAGES\\FermetureStandard B_" + k + " ES_" + i + ".tiff");
                }

                List<int[,]> gNewFerme = new List<int[,]>();
                List<int[,]> gNewOuvert = new List<int[,]>();
                Reconstruction(imagesMat, imagesErodeInit, imagesDilateInit, poids, itStabilite, prStabilite, ref gNewOuvert, ref gNewFerme);

                List<Bitmap> fermeBmp = new List<Bitmap>();
                List<Bitmap> ouvertBmp = new List<Bitmap>();
                for (int k = 0; k < imagesBmp.Count; k++)
                {
                    Bitmap sortieFerme = new Bitmap(imagesBmp[k].Width, imagesBmp[k].Height);
                    Bitmap sortieOuvert = new Bitmap(imagesBmp[k].Width, imagesBmp[k].Height);
                    for (int x = 0; x < imagesBmp[k].Width; x++)
                    {
                        for (int y = 0; y < imagesBmp[k].Height; y++)
                        {
                            sortieFerme.SetPixel(x, y, Color.FromArgb(gNewFerme[k][x, y], gNewFerme[k][x, y], gNewFerme[k][x, y]));
                            sortieOuvert.SetPixel(x, y, Color.FromArgb(gNewOuvert[k][x, y], gNewOuvert[k][x, y], gNewOuvert[k][x, y]));
                        }
                    }
                    fermeBmp.Add(sortieFerme);
                    ouvertBmp.Add(sortieOuvert);
                    sortieFerme.Save(".\\IMAGES\\FermeReconstruction B_" + k + " ES_" + i + ".tiff");
                    sortieOuvert.Save(".\\IMAGES\\OuvertReconstruction B_" + k + " ES_" + i + ".tiff");
                }
            }
        }

        private void Reconstruction(List<int[,]> imagesMat, List<int[,]> imagesErodeInit, List<int[,]> imagesDilateInit, double[] poids, int itStabilite, Double prStabilite, ref List<int[,]> gNewOuvert, ref List<int[,]> gNewFerme)
        {
            bool stopErod = false, stopDilat = false;
            List<int[,]> gLastFerme = new List<int[,]>();
            List<int[,]> gLastOuvert = new List<int[,]>();
            for (int k = 0; k < imagesMat.Count; k++)
            {
                gNewFerme.Add(new int[imagesMat[0].GetLength(0), imagesMat[0].GetLength(1)]);
                gNewOuvert.Add(new int[imagesMat[0].GetLength(0), imagesMat[0].GetLength(1)]);
                gLastOuvert.Add((int[,])imagesErodeInit[k].Clone());
                gLastFerme.Add((int[,])imagesDilateInit[k].Clone());
            }
            int toleranceStabilite = 0;
            if (itStabilite == 0) itStabilite = int.MaxValue;
            while (((!stopDilat) || (!stopErod)) && (toleranceStabilite < itStabilite))
            {
                stopErod = true; stopDilat = true;
                ErosionDilatation(imagesMat, gLastFerme, gLastOuvert, poids, prStabilite, ref gNewFerme, ref gNewOuvert, ref stopErod, ref stopDilat);
                for (int k = 0; k < imagesMat.Count; k++)
                {
                    gLastOuvert[k] = (int[,])gNewOuvert[k].Clone();
                    gLastFerme[k] = (int[,])gNewFerme[k].Clone();
                }
                toleranceStabilite++;
            }
        }

        private void ErosionDilatation(List<int[,]> imagesMat, List<int[,]> gLastFerme, List<int[,]> gLastOuvert, double[] poids, Double prStabilite, ref List<int[,]> gNewFerme, ref List<int[,]> gNewOuvert, ref bool stopErod, ref bool stopDilat)
        {
            int ressemblanceOuverture = 0, ressemblanceFermeture = 0;
            for (int x = 0; x < imagesMat[0].GetLength(0); x++)
            {
                for (int y = 0; y < imagesMat[0].GetLength(1); y++)
                {
                    if ((x < 1) || (y < 1) || (x > imagesMat[0].GetLength(0) - 2) || (y > imagesMat[0].GetLength(1) - 2))
                    {
                        for (int k = 0; k < imagesMat.Count; k++)
                        {
                            gNewFerme[k][x, y] = gLastFerme[k][x, y];
                            gNewOuvert[k][x, y] = gLastOuvert[k][x, y];
                        }
                    }
                    else
                    {
                        int sMax = 0, tMax = 0, sMin = 0, tMin = 0;
                        MinMaxVecteurs(gLastFerme, gLastOuvert, poids, x, y, ref sMin, ref tMin, ref sMax, ref tMax);

                        if (Norme(imagesMat, x, y) < Norme(gLastFerme, sMin, tMin))
                        {
                            for (int i = 0; i < imagesMat.Count; i++) gNewFerme[i][x, y] = gLastFerme[i][sMin, tMin];
                        }
                        else if (Norme(imagesMat, x, y) > Norme(gLastFerme, sMin, tMin))
                        {
                            for (int i = 0; i < imagesMat.Count; i++) gNewFerme[i][x, y] = imagesMat[i][x, y];
                        }
                        else
                        {
                            int cpt = 0;
                            for (int k = 0; k < imagesMat.Count; k++)
                            {
                                if (imagesMat[k][x, y] < gLastFerme[k][sMin, tMin]) cpt++;
                                else if (imagesMat[k][x, y] > gLastFerme[k][sMin, tMin]) cpt--;
                            }
                            if (cpt > 0) for (int i = 0; i < imagesMat.Count; i++) gNewFerme[i][x, y] = gLastFerme[i][sMin, tMin];
                            else if (cpt < 0) for (int i = 0; i < imagesMat.Count; i++) gNewFerme[i][x, y] = imagesMat[i][x, y];
                            else
                            {
                                for (int k = 0; k < imagesMat.Count; k++)
                                {
                                    if (imagesMat[k][x, y] < gLastFerme[k][sMin, tMin])
                                    {
                                        for (int i = 0; i < imagesMat.Count; i++) gNewFerme[i][x, y] = gLastFerme[i][sMin, tMin];
                                        break;
                                    }
                                    else if (imagesMat[k][x, y] > gLastFerme[k][sMin, tMin])
                                    {
                                        for (int i = 0; i < imagesMat.Count; i++) gNewFerme[i][x, y] = imagesMat[i][x, y];
                                        break;
                                    }
                                    else if (k == imagesMat.Count - 1) for (int i = 0; i < imagesMat.Count; i++) gNewFerme[i][x, y] = imagesMat[i][x, y];
                                }
                            }

                        }

                        if (Norme(imagesMat, x, y) > Norme(gLastOuvert, sMax, tMax))
                        {
                            for (int i = 0; i < imagesMat.Count; i++) gNewOuvert[i][x, y] = gLastOuvert[i][sMax, tMax];
                        }
                        else if (Norme(imagesMat, x, y) < Norme(gLastOuvert, sMax, tMax))
                        {
                            for (int i = 0; i < imagesMat.Count; i++) gNewOuvert[i][x, y] = imagesMat[i][x, y];
                        }
                        else
                        {
                            int cpt = 0;
                            for (int k = 0; k < imagesMat.Count; k++)
                            {
                                if (imagesMat[k][x, y] > gLastOuvert[k][sMax, tMax]) cpt--;
                                else if (imagesMat[k][x, y] < gLastOuvert[k][sMax, tMax]) cpt++;
                            }
                            if (cpt > 0) for (int i = 0; i < imagesMat.Count; i++) gNewOuvert[i][x, y] = imagesMat[i][x, y];
                            else if (cpt < 0) for (int i = 0; i < imagesMat.Count; i++) gNewOuvert[i][x, y] = gLastOuvert[i][sMax, tMax];
                            else
                            {
                                for (int k = 0; k < imagesMat.Count; k++)
                                {
                                    if (imagesMat[k][x, y] > gLastOuvert[k][sMax, tMax])
                                    {
                                        for (int i = 0; i < imagesMat.Count; i++) gNewOuvert[i][x, y] = gLastOuvert[i][sMax, tMax];
                                        break;
                                    }
                                    else if (imagesMat[k][x, y] < gLastOuvert[k][sMax, tMax])
                                    {
                                        for (int i = 0; i < imagesMat.Count; i++) gNewOuvert[i][x, y] = imagesMat[i][x, y];
                                        break;
                                    }
                                    else if (k == imagesMat.Count - 1)
                                    {
                                        for (int i = 0; i < imagesMat.Count; i++) gNewOuvert[i][x, y] = imagesMat[i][x, y];
                                    }
                                }
                            }
                        }
                        int ressemblanceOuvertureLocale = 0, ressemblanceFermetureLocale = 0;
                        for (int k = 0; k < imagesMat.Count; k++)
                        {
                            if (gNewFerme[k][x, y] != gLastFerme[k][x, y]) stopErod = false;
                            else ressemblanceFermetureLocale++;
                            if (gNewOuvert[k][x, y] != gLastOuvert[k][x, y]) stopDilat = false;
                            else ressemblanceOuvertureLocale++;
                        }
                        if (ressemblanceOuvertureLocale == imagesMat.Count) ressemblanceOuverture++;
                        if (ressemblanceFermetureLocale == imagesMat.Count) ressemblanceFermeture++;
                    }
                }
            }
            Double tauxRessemblanceOuverture = (Double)ressemblanceOuverture / (imagesMat[0].GetLength(0) * imagesMat[0].GetLength(1));
            Double tauxRessemblanceFermeture = (Double)ressemblanceFermeture / (imagesMat[0].GetLength(0) * imagesMat[0].GetLength(1));
            Console.WriteLine(tauxRessemblanceOuverture.ToString() + " " + tauxRessemblanceFermeture.ToString());
            if (tauxRessemblanceOuverture >= prStabilite) stopDilat = true;
            if (tauxRessemblanceFermeture >= prStabilite) stopErod = true;
        }

        private double Norme(List<int[,]> imagesMat, int x, int y)
        {
            double norme = 0;
            for (int k = 0; k < imagesMat.Count; k++) norme += imagesMat[k][x, y];
            return norme;
        }

        private void OuvertureFermetureStandard(List<int[,]> imagesErodeInit, List<int[,]> imagesDilateInit, double[] poids, ref List<int[,]> imagesOuvertesStandards, ref List<int[,]> imagesFermeesStandards, int i)
        {
            List<int[,]> imagesErodPrec = new List<int[,]>();
            List<int[,]> imagesDilatePrec = new List<int[,]>();
            for (int k = 0; k < imagesErodeInit.Count; k++)
            {
                imagesOuvertesStandards.Add(new int[imagesErodeInit[0].GetLength(0), imagesErodeInit[0].GetLength(1)]);
                imagesFermeesStandards.Add(new int[imagesErodeInit[0].GetLength(0), imagesErodeInit[0].GetLength(1)]);
                imagesErodPrec.Add((int[,])imagesDilateInit[k].Clone());
                imagesDilatePrec.Add((int[,])imagesErodeInit[k].Clone());
            }
            for (int elemStruct = 0; elemStruct < i; elemStruct++)
            {
                for (int x = 0; x < imagesErodeInit[0].GetLength(0); x++)
                {
                    for (int y = 0; y < imagesErodeInit[0].GetLength(1); y++)
                    {
                        if ((x < 1) || (y < 1) || (x > imagesErodeInit[0].GetLength(0) - 2) || (y > imagesErodeInit[0].GetLength(1) - 2))
                        {
                            for (int k = 0; k < imagesErodeInit.Count; k++)
                            {
                                imagesOuvertesStandards[k][x, y] = imagesDilatePrec[k][x, y];
                                imagesFermeesStandards[k][x, y] = imagesErodPrec[k][x, y];
                            }
                        }
                        else
                        {
                            int sMax = 0, tMax = 0, sMin = 0, tMin = 0;
                            MinMaxVecteurs(imagesErodPrec, imagesDilatePrec, poids, x, y, ref sMin, ref tMin, ref sMax, ref tMax);
                            for (int k = 0; k < imagesErodeInit.Count; k++)
                            {
                                imagesFermeesStandards[k][x, y] = imagesErodPrec[k][sMin, tMin];
                                imagesOuvertesStandards[k][x, y] = imagesDilatePrec[k][sMax, tMax];
                            }
                        }
                    }
                }
                for (int k = 0; k < imagesErodeInit.Count; k++)
                {
                    imagesErodPrec[k] = (int[,])imagesFermeesStandards[k].Clone();
                    imagesDilatePrec[k] = (int[,])imagesOuvertesStandards[k].Clone();
                }
            }
        }

        private void ErosionDilatationInit(List<int[,]> imagesMat, double[] poids, ref List<int[,]> imagesErodeInit, ref List<int[,]> imagesDilateInit, int i)
        {
            List<int[,]> imagesErodPrec = new List<int[,]>();
            List<int[,]> imagesDilatePrec = new List<int[,]>();
            for (int k = 0; k < imagesMat.Count; k++)
            {
                imagesDilateInit.Add(new int[imagesMat[0].GetLength(0), imagesMat[0].GetLength(1)]);
                imagesErodeInit.Add(new int[imagesMat[0].GetLength(0), imagesMat[0].GetLength(1)]);
                imagesErodPrec.Add((int[,])imagesMat[k].Clone());
                imagesDilatePrec.Add((int[,])imagesMat[k].Clone());
            }
            for (int elemStruct = 0; elemStruct < i; elemStruct++)
            {
                for (int x = 0; x < imagesMat[0].GetLength(0); x++)
                {
                    for (int y = 0; y < imagesMat[0].GetLength(1); y++)
                    {
                        if ((x < 1) || (y < 1) || (x > imagesMat[0].GetLength(0) - 2) || (y > imagesMat[0].GetLength(1) - 2))
                        {
                            for (int k = 0; k < imagesMat.Count; k++)
                            {
                                imagesDilateInit[k][x, y] = imagesDilatePrec[k][x, y];
                                imagesErodeInit[k][x, y] = imagesErodPrec[k][x, y];
                            }
                        }
                        else
                        {
                            int sMax = 0, tMax = 0, sMin = 0, tMin = 0;
                            MinMaxVecteurs(imagesErodPrec, imagesDilatePrec, poids, x, y, ref sMin, ref tMin, ref sMax, ref tMax);
                            for (int k = 0; k < imagesMat.Count; k++)
                            {
                                imagesErodeInit[k][x, y] = imagesErodPrec[k][sMin, tMin];
                                imagesDilateInit[k][x, y] = imagesDilatePrec[k][sMax, tMax];
                            }
                        }
                    }
                }
                for (int k = 0; k < imagesMat.Count; k++)
                {
                    imagesErodPrec[k] = (int[,])imagesErodeInit[k].Clone();
                    imagesDilatePrec[k] = (int[,])imagesDilateInit[k].Clone();
                }
            }
        }

        private void MinMaxVecteurs(List<int[,]> imagesErodPrec, List<int[,]> imagesDilatePrec, double[] poids, int x, int y, ref int sMin, ref int tMin, ref int sMax, ref int tMax)
        {
            double[,] preferenceGlobaleErod = new double[9, 9];
            double[,] preferenceGlobaleDilat = new double[9, 9];
            for (int s = x - 1; s <= x + 1; s++)
            {
                for (int t = y - 1; t <= y + 1; t++)
                {
                    for (int ss = x - 1; ss <= x + 1; ss++)
                    {
                        for (int tt = y - 1; tt <= y + 1; tt++)
                        {
                            for (int k = 0; k < imagesErodPrec.Count; k++)
                            {
                                double PkErod = 0, PkDilat = 0;
                                if (imagesErodPrec[k][s, t] - imagesErodPrec[k][ss, tt] >= 0) PkErod = 0;
                                else
                                {
                                    double distanceErod = Math.Abs(imagesErodPrec[k][s, t] - imagesErodPrec[k][ss, tt]);
                                    if (distanceErod / imagesErodPrec[k][ss, tt] <= 0.1) PkErod = 0;
                                    else PkErod = 1;
                                }
                                if (imagesDilatePrec[k][s, t] - imagesDilatePrec[k][ss, tt] <= 0) PkDilat = 0;
                                else
                                {
                                    double distanceDilat = imagesDilatePrec[k][s, t] - imagesDilatePrec[k][ss, tt];
                                    if (distanceDilat / imagesDilatePrec[k][s, t] <= 0.1) PkDilat = 0;
                                    else PkDilat = 1;
                                }
                                preferenceGlobaleErod[3 * (s - (x - 1)) + t - (y - 1), 3 * (ss - (x - 1)) + tt - (y - 1)] += PkErod * poids[k];
                                preferenceGlobaleDilat[3 * (s - (x - 1)) + t - (y - 1), 3 * (ss - (x - 1)) + tt - (y - 1)] += PkDilat * poids[k];
                            }
                            preferenceGlobaleErod[3 * (s - (x - 1)) + t - (y - 1), 3 * (ss - (x - 1)) + tt - (y - 1)] = preferenceGlobaleErod[3 * (s - (x - 1)) + t - (y - 1), 3 * (ss - (x - 1)) + tt - (y - 1)] / poids.Sum();
                            preferenceGlobaleDilat[3 * (s - (x - 1)) + t - (y - 1), 3 * (ss - (x - 1)) + tt - (y - 1)] = preferenceGlobaleDilat[3 * (s - (x - 1)) + t - (y - 1), 3 * (ss - (x - 1)) + tt - (y - 1)] / poids.Sum();
                        }
                    }
                }
            }

            double fluxNetErod = 0, fluxNetDilat = 0;
            for (int i = 0; i < 9; i++)
            {
                double fluxNetErodTmp = 0, fluxNetDilatTmp = 0;
                for (int j = 0; j < 9; j++)
                {
                    fluxNetErodTmp += (preferenceGlobaleErod[i, j] - preferenceGlobaleErod[j, i]) / 8;
                    fluxNetDilatTmp += (preferenceGlobaleDilat[i, j] - preferenceGlobaleDilat[j, i]) / 8;
                }
                if (fluxNetErodTmp > fluxNetErod)
                {
                    fluxNetErod = fluxNetErodTmp;
                    sMin = (x - 1) + (i / 3);
                    tMin = (y - 1) + (i % 3);
                }
                else if (fluxNetErodTmp == fluxNetErod)
                {
                    for (int k = 0; k < imagesErodPrec.Count; k++)
                    {
                        if (imagesErodPrec[k][i / 3, i % 3] > imagesErodPrec[k][sMin, tMin]) break;
                        else if (imagesErodPrec[k][i / 3, i % 3] < imagesErodPrec[k][sMin, tMin])
                        {
                            sMin = (x - 1) + (i / 3);
                            tMin = (y - 1) + (i % 3);
                            break;
                        }
                    }
                }
                if (fluxNetDilatTmp > fluxNetDilat)
                {
                    fluxNetDilat = fluxNetDilatTmp;
                    sMax = (x - 1) + (i / 3);
                    tMax = (y - 1) + (i % 3);
                }
                else if (fluxNetDilatTmp == fluxNetDilat)
                {
                    for (int k = 0; k < imagesDilatePrec.Count; k++)
                    {
                        if (imagesDilatePrec[k][i / 3, i % 3] < imagesDilatePrec[k][sMax, tMax]) break;
                        else if (imagesDilatePrec[k][i / 3, i % 3] > imagesDilatePrec[k][sMax, tMax])
                        {
                            sMax = (x - 1) + (i / 3);
                            tMax = (y - 1) + (i % 3);
                            break;
                        }
                    }
                }
            }
        }

        private List<int[,]> bmpToMat(List<Bitmap> imagesBmp)
        {
            List<int[,]> imagesMat = new List<int[,]>();
            for (int z = 0; z < imagesBmp.Count; z++)
            {
                imagesMat.Add(new int[imagesBmp[z].Width, imagesBmp[z].Height]);
                for (int x = 0; x < imagesBmp[z].Width; x++)
                {
                    for (int y = 0; y < imagesBmp[z].Height; y++)
                    {
                        Color pixelColor = imagesBmp[z].GetPixel(x, y);
                        imagesMat[z][x, y] = pixelColor.R;
                    }
                }
            }
            return imagesMat;
        }
    }
}
