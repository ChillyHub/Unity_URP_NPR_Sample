using System;
using JetBrains.RiderFlow.Core.Infrastructure;
using JetBrains.RiderFlow.Core.Infrastructure.Tools;
using JetBrains.RiderFlow.Core.Threading;
using UnityEngine;

namespace JetBrains.RiderFlow.Since2019_4
{
    public static class BackendInstallationProgress
    {
        public static void Initialize()
        {
            BackendDownloadProgressManager.Create = () => new BackendDownloadProgress();
            BackendExtractProgressManager.Create = () => new BackendExtractProgress();
        }

        private class BackendDownloadProgress : DownloadTools.IDownloadProgress
        {
            private readonly IntervalProgress myProgress = new IntervalProgress();

            public void Start()
            {
                MainThreadScheduler.Instance.Queue(ShowStartedDownloadingMessage);
            }

            public void UpdateOnEachDownloadedMegabyte(int downloadedMegabytes, int totalMegabytes,
                                                       int downloadSpeedInMegabytesPerSecond)
            {
                MainThreadScheduler.Instance.Queue(
                    () => UpdateProgress(downloadedMegabytes, totalMegabytes, downloadSpeedInMegabytesPerSecond));
            }

            public void Finish()
            {
                Debug.Log("RiderFlow: Finished downloading backend service archive");
            }

            private static void ShowStartedDownloadingMessage()
            {
                Debug.Log("RiderFlow: Started downloading backend service archive");
            }

            private void UpdateProgress(int downloadedMegabytes, int totalMegabytes, int downloadSpeedInMegabytes)
            {
                myProgress.ShowProgressInIntervalWithSpeed(downloadedMegabytes, totalMegabytes,
                    downloadSpeedInMegabytes, ShowProgress);
            }

            private static void ShowProgress(int downloadedMegabytes, int totalMegabytes, int downloadSpeedInMegabytes)
            {
                Debug.Log(
                    $"RiderFlow: Downloaded {downloadedMegabytes}/{totalMegabytes} MB of backend service archive. " +
                    $"Speed is {downloadSpeedInMegabytes} MB/s.");
            }
        }

        private class BackendExtractProgress : ExtractTools.IExtractProgress
        {
            private readonly IntervalProgress myProgress = new IntervalProgress();

            public void Start()
            {
                MainThreadScheduler.Instance.Queue(ShowExtractionStartedMessage);
            }

            public void OnEachExtractedFile(int extractedFilesCount, int totalExtractedFiles)
            {
                MainThreadScheduler.Instance.Queue(() => UpdateProgress(extractedFilesCount, totalExtractedFiles));
            }

            public void Finish()
            {
                Debug.Log("RiderFlow: Finished extracting backend service archive");
            }

            private static void ShowExtractionStartedMessage()
            {
                Debug.Log("RiderFlow: Started extracting backend service archive");
            }

            private void UpdateProgress(int extractedFilesCount, int totalExtractedFiles)
            {
                myProgress.ShowProgressInInterval(extractedFilesCount, totalExtractedFiles, ShowProgress);
            }

            private static void ShowProgress(int extractedFilesCount, int totalExtractedFiles)
            {
                Debug.Log(
                    $"RiderFlow: Extracted {extractedFilesCount}/{totalExtractedFiles} files of backend service archive");
            }
        }

        private class IntervalProgress
        {
            private readonly bool[] myIntervals;

            public IntervalProgress(int intervalsCount = 10)
            {
                intervalsCount = Math.Max(Math.Min(intervalsCount, 100), 1);
                myIntervals = new bool[intervalsCount];
            }

            public void ShowProgressInInterval(int processed, int total, Action<int, int> showProgress)
            {
                var intervalIndex = CalculateIntervalIndex(processed, total);
                if (!ValidInterval(intervalIndex)) return;
                showProgress(processed, total);
                myIntervals[intervalIndex] = true;
            }

            public void ShowProgressInIntervalWithSpeed(int processed, int total, int speed,
                                                        Action<int, int, int> showProgress)
            {
                var intervalIndex = CalculateIntervalIndex(processed, total);
                if (!ValidInterval(intervalIndex)) return;
                showProgress(processed, total, speed);
                myIntervals[intervalIndex] = true;
            }

            private bool ValidInterval(int intervalIndex)
            {
                return intervalIndex >= 0 && intervalIndex < myIntervals.Length && !myIntervals[intervalIndex];
            }

            private int CalculateIntervalIndex(int processed, int total)
            {
                var processedFraction = (float)processed / total;
                var processedInPercents = (int)(processedFraction * 100);
                return processedInPercents / myIntervals.Length;
            }
        }
    }
}