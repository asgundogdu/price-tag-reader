﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;

namespace PybossaParser
{
    class Program
    {
        static void Main(string[] args)
        {
            Environment.CurrentDirectory = @"D:\GitHub\price-tag-reader\pybossa\parser\Test";
            const string inputFolder = "input", outputFolder = "output", outputFolderCrops = "cropped", inputTaskFile = "task.txt", inputTaskRunFile = "task_run.txt", outputFile = "areas.txt";

            Regex regexTask = new Regex(@"(?<id>\d+).*?""url_raw"": ""(?<url>.*)"""), regexTaskRun = new Regex(@"(?<idcrop>\d+)\t.*?\t.*?\t(?<id>\d+)\t.*\[(?<areas>.*)\]"), 
                regexAreas = new Regex(@"""y"": (?<y>[\.\d]+).*?""x"": (?<x>[\.\d]+).*?""height"": (?<height>[\.\d]+).*?""width"": (?<width>[\.\d]+).*?""type"": ""(?<type>.*?)""");

            if (!Directory.Exists(outputFolder))
                Directory.CreateDirectory(outputFolder);

            // Download the images
            Console.WriteLine("Downloading images");
        
            foreach (var line in File.ReadAllLines(Path.Combine(inputFolder,inputTaskFile)))
            {
                var m = regexTask.Match(line);

                if (m.Success)
                {
                    var f = Path.Combine(outputFolder, $"{m.Groups["id"].Value}.jpg");
                    Console.WriteLine("Getting: " + f);

                    if (File.Exists(f))
                        continue;

                    var url = m.Groups["url"].Value;
                    try
                    {
                        new WebClient().DownloadFile(url, f);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine($"Error downloading: {url} as {f}, details: {ex.Message}");
                    }
                }
            }

            // Parsing results
            var output = new List<string>();
            Console.WriteLine("Processing tasks");
            foreach (var line in File.ReadAllLines(Path.Combine(inputFolder, inputTaskRunFile)))
            {
                var m = regexTaskRun.Match(line);

                if(m.Success)
                {
                    var filename = $"{m.Groups["id"].Value}.jpg";
                    var id = m.Groups["idcrop"].Value;
                    var mAreas = regexAreas.Matches(m.Groups["areas"].Value);

                    if (mAreas.Count == 0)
                        Console.WriteLine("Image without areas!");
                    else
                    {
                        foreach (Match area in mAreas)
                        {
                            // Get image
                            var imageFile = Path.Combine(outputFolder, filename);

                            Console.WriteLine("Processing task for: " + filename);
                            if (!File.Exists(imageFile))
                            {
                                Console.WriteLine("Task image is not available: " + filename);
                            }
                            else
                            {
                                var img = Image.FromFile(imageFile);

                                var width = img.Width;
                                var height = img.Height;

                                var type = area.Groups["type"].Value;

                                // Get correct proportions
                                var rect = new RectangleF(float.Parse(area.Groups["x"].Value) * width, float.Parse(area.Groups["y"].Value) * height, float.Parse(area.Groups["width"].Value) * width, float.Parse(area.Groups["height"].Value) * height);

                                // Using ['filename', 'width', 'height', 'class', 'xmin', 'ymin', 'xmax', 'ymax']
                                output.Add($"['{filename}', '{width}', '{height}', '{type}', '{(int)rect.Left}', '{(int)rect.Top}', '{(int)rect.Right}', '{(int)rect.Bottom}']");

                                // Cropping the picture
                                var c = Path.Combine(outputFolder, outputFolderCrops);
                                if (!Directory.Exists(c))
                                    Directory.CreateDirectory(c);

                                var outputImageCropsFolder = Path.Combine(c, Path.GetFileNameWithoutExtension(imageFile));
                                if (!Directory.Exists(outputImageCropsFolder))
                                    Directory.CreateDirectory(outputImageCropsFolder);

                                var outputImageCropsSource = Path.Combine(outputImageCropsFolder, Path.GetFileName(imageFile));
                                if (!File.Exists(outputImageCropsSource))
                                    img.Save(outputImageCropsSource);

                                // Save crop
                                CropImage(img, rect).Save(Path.Combine(outputImageCropsFolder, $"crop_{id}.jpg"));
                               
                            }
                        }
                    }
                }
            }

            var o = Path.Combine(outputFolder, outputFile);

            if (File.Exists(o))
                File.Delete(o);

            Console.WriteLine($"Writing: {outputFile}");
            File.WriteAllLines(o, output.ToArray());
            Console.WriteLine("All done.");
        }

        // From: https://stackoverflow.com/questions/734930/how-to-crop-an-image-using-c
        private static Image CropImage(Image img, RectangleF cropArea)
        {
            Bitmap bmpImage = new Bitmap(img);
            return bmpImage.Clone(cropArea, bmpImage.PixelFormat);
        }
    }
}
