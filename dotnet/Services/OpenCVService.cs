using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Geste.Messaging;
using OpenCvSharp;
using Serilog.Core;

namespace Geste.Services
{
    public record PointContainer(Point Point, int Idx);

    public record DefectVertex(Point point, Point d1, Point d2);

    public class OpenCVService
    {
        private readonly ArduinoService _arduinoService;
        private readonly ConfigService _configService;

        private readonly Logger _logger;
        private readonly Vec3b SkinLower;
        private readonly Vec3b SkinUpper;

        public OpenCVService(ArduinoService arduinoService, ConfigService configService, Logger logger)
        {
            _arduinoService = arduinoService;
            _configService = configService;
            _logger = logger;

            var hHigh = _configService.Config.HSLSkinHigh;
            var hLow = _configService.Config.HSLSkinLow;


            var h_high = (byte) (hHigh[0] / 2);
            var s_high = (byte) ((double) hHigh[1] / 100 * 255);
            var l_high = (byte) ((double) hHigh[2] / 100 * 255);

            var h_low = (byte) (hLow[0] / 2);
            var s_low = (byte) ((double) hLow[1] / 100 * 255);
            var l_low = (byte) ((double) hLow[2] / 100 * 255);

            SkinUpper = new Vec3b(h_high, s_high, l_high);
            SkinLower = new Vec3b(h_low, s_low, l_low);
        }

        // pokrecemo citanje feeda koji je naveden u configu
        public async Task StartAsync()
        {
            _logger.Information("Spajam se na feed.");
            GrabFeed(HandleFrame);
        }

        // detektiranje prstiju, crtanje rezultata i binary maske
        private void HandleFrame(Mat frame)
        {
            frame.Resize(640);

            var mask = CreateHandMask(frame);
            var contour = CreateHandContour(mask);

            var hullIndices = CreateRoughHull(contour, 25).ToArray();

            var vertices = CreateHullDefectVertices(contour, hullIndices);
            var verticesAngled = FilterVerticesWithAngle(vertices, 60);

            var result = frame.Clone();

            var defectVertices = verticesAngled as DefectVertex[] ?? verticesAngled.ToArray();

            var fingers = defectVertices.Length;
            _arduinoService.Write(new HandUpdateMessage(fingers, contour != null, hullIndices.Length > 0));

            if (!_configService.Config.ShowFeed) return;

            if (contour != null)
                frame.DrawContours(new[] {contour}, -1, Scalar.BlueViolet, 2);

            foreach (var vertex in defectVertices)
            {
                frame.Line(vertex.point, vertex.d1, Scalar.Aquamarine, 2);
                frame.Line(vertex.point, vertex.d2, Scalar.Aquamarine, 2);
                frame.Ellipse(new RotatedRect(vertex.point, new Size2f(20, 20), 0), Scalar.Blue, 2);
                result.Ellipse(new RotatedRect(vertex.point, new Size2f(20, 20), 0), Scalar.Blue, 2);
            }

            result.PutText(fingers.ToString(), new Point(20, result.Height - 60), HersheyFonts.HersheySimplex,
                2,
                Scalar.DimGray, 2, LineTypes.AntiAlias);


            var rows = result.Rows;
            var cols = result.Cols;

            using var sideBySide = new Mat(rows, cols * 2, MatType.CV_8UC3);
            using var r = new Mat(sideBySide, new Rect(0, 0, cols, rows));
            using var f = new Mat(sideBySide, new Rect(cols, 0, cols, rows));


            result.CopyTo(r);
            frame.CopyTo(f);

            Cv2.ImShow("MASKA", mask);
            Cv2.ImShow("REZULTAT", sideBySide);
        }

        // uzimanje slike iz feeda WaitKey delayem od 20ms
        private void GrabFeed(Action<Mat> handleFrame)
        {
            var capture = new VideoCapture(_configService.Config.VideoSource);

            while (true)
            {
                using var matFrame = new Mat();
                if (!capture.Read(matFrame)) continue;
                handleFrame(matFrame);
                var key = Cv2.WaitKey(20);

                if (key == -1 || key == 255) continue;
                capture.Release();
                break;
            }
        }

        // kreiramo masku prema HLS valutama navedenim u konfiguraciji
        // zatim masku malo zamutimo da bi se riješili nečistoća
        // i imamo threshold radi boljeg filtriranja
        private Mat CreateHandMask(Mat frame)
        {
            var HLS = frame.CvtColor(ColorConversionCodes.BGR2HLS);
            var range = HLS.InRange(SkinLower, SkinUpper);

            var blur = range.Blur(new Size(10, 10));
            var threshold = blur.Threshold(200, 255, ThresholdTypes.Binary);

            return threshold;
        }

        // radimo konturu i uzimamo silazni poredak prema veličini konture
        // te uzimamo prvu konturu u toj kolekciji (najveća kontura)
        private Point[] CreateHandContour(Mat mask)
        {
            mask.FindContours(out var contours, out var hierarchyIndices, RetrievalModes.External,
                ContourApproximationModes.ApproxSimple);

            return contours.OrderByDescending(x => Cv2.ContourArea(x)).FirstOrDefault();
        }

        // helper metoda za pronalaženje centra među više točaka
        private Point CenterPoint(IEnumerable<Point> points)
        {
            var p = points.Aggregate(new Point(0, 0), (sum, point) => sum.Add(point));
            return p.ToVec2i().Divide(points.Count());
        }

        // kreiranje rubova oko ruke
        private IEnumerable<int> CreateRoughHull(Point[] contour, int maxDistance)
        {
            if (contour == null) return Enumerable.Empty<int>();

            var indices = Cv2.ConvexHullIndices(contour).OrderByDescending(x => x);
            var pointsIdx = indices
                .Select(x => new PointContainer(contour[x], x))
                .ToArray();

            var hullPoints = pointsIdx.Select(x => x.Point);

            Cv2.Partition(hullPoints, out var labels, (p1, p2) => Point.Distance(p1, p2) < maxDistance);

            var dict = new Dictionary<int, List<PointContainer>>();

            for (var index = 0; index < labels.Length; index++)
            {
                var label = labels[index];
                dict[label] = new List<PointContainer>();
            }


            for (var i = 0; i < pointsIdx.Count(); i++)
            {
                var idx = pointsIdx[i];
                var label = labels[i];

                if (dict.TryGetValue(label, out var list))
                    list.Add(idx);
            }

            var pointGroups = dict.Values.ToArray();

            return pointGroups
                .Select(x =>
                {
                    var center = CenterPoint(x.Select(x => x.Point));

                    return x.OrderBy(x => Point.Distance(x.Point, center)).FirstOrDefault().Idx;
                });
        }

        // uzimamo vrhove koji nisu pravi vrhovi (imaju smetnje i sl.)
        private IEnumerable<DefectVertex> CreateHullDefectVertices(Point[] contour, IEnumerable<int> indices)
        {
            if (contour == null || !indices.Any()) return Enumerable.Empty<DefectVertex>();

            indices = indices.OrderByDescending(x => x);
            var defects = Cv2.ConvexityDefects(contour, indices);
            var neighbours = new Dictionary<int, List<int>>();
            foreach (var induce in indices) neighbours[induce] = new List<int>();

            foreach (var defect in defects)
            {
                var startPoint = defect.Item0;
                var endPoint = defect.Item1;
                var defectPoint = defect.Item2;

                neighbours[startPoint].Add(defectPoint);
                neighbours[endPoint].Add(defectPoint);
            }

            return neighbours.Keys
                .Where(x => neighbours[x].Count > 1)
                .Select(hull =>
                {
                    var defectNeighbourId = neighbours[hull];

                    return new DefectVertex(contour[hull], contour[defectNeighbourId[0]],
                        contour[defectNeighbourId[1]]);
                });
        }

        // filtriramo vrhove zato što znamo da prsti imaju oštar vrh
        private IEnumerable<DefectVertex> FilterVerticesWithAngle(IEnumerable<DefectVertex> vertices, int maxAngle)
        {
            return vertices.Where(x =>
            {
                var vert1 = Cv2.Norm(InputArray.Create(x.d1.Subtract(x.d2).ToVec2i()));
                var vert2 = Cv2.Norm(InputArray.Create(x.point.Subtract(x.d1).ToVec2i()));
                var vert3 = Cv2.Norm(InputArray.Create(x.point.Subtract(x.d2).ToVec2i()));

                var deg = Math.Acos((vert2 * vert2 + vert3 * vert3 - vert1 * vert1) / (2 * vert2 * vert3)) *
                          (180 / Math.PI);

                return deg < maxAngle;
            });
        }
    }
}