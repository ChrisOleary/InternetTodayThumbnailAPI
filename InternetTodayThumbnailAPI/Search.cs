using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration.UserSecrets;

namespace InternetTodayThumbnailAPI
{
    internal class Search
    {
        [STAThread]
        static void Main(string[] args)
        {

            Console.WriteLine("YouTube Data API: Search");
            Console.WriteLine("==========PROCESSING......=======");

            try
            {
                new Search().Run().Wait();
            }
            catch (AggregateException ex)
            {
                foreach (var e in ex.InnerExceptions) 
                {
                    Console.WriteLine("Error: " + e.Message);
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        private async Task Run()
        {
            // variables
            List<string> videos = new List<string>();
            List<string> channels = new List<string>();
            List<string> thumbnails = new List<string>();
            List<string> publishedAt = new List<string>();
            List<string> videoId = new List<string>();

            var folder = $"{Directory.GetParent("Images").Parent.Parent.Parent.FullName}\\Images";

            DateTime tomorrowsDate = DateTime.Today.AddDays(1);
            DateTime? lastItemInListPublishedDate;
            DateTime? firstItemInListPublishedDate;

            int countOfList;
            int totalFileCount = 0;

            // initialize youtube service and pass in filters
            var youtubeService = new YouTubeService(new BaseClientService.Initializer()
            {
                ApiKey = "AIzaSyCOCEoet-VxdL_PAdupTMQki-y_SCdsyxk",
                ApplicationName = this.GetType().ToString()
            });
            var searchListRequest = youtubeService.Search.List("snippet");
            searchListRequest.ChannelId = "UCyZVCV9xhrCyz4hPehvb4Wg"; // Internet Todays Channel ID
            searchListRequest.MaxResults = 50; // max records that api will return in one request
            searchListRequest.Order = (SearchResource.ListRequest.OrderEnum?)1; // order by latest date

            searchListRequest.PublishedBefore = tomorrowsDate;

            // First call to the search.list method to retrieve results matching the specified query term.
            var searchListResponse = await searchListRequest.ExecuteAsync();

            // get count of list. If 0 then theres no more to loop through
            countOfList = searchListResponse.Items.Count();

            do
            {
                var lastItemInList = searchListResponse.Items[countOfList - 1];
                lastItemInListPublishedDate = lastItemInList.Snippet.PublishedAt;

                // get first date of item in batch
                var firstItemInList = searchListResponse.Items[0];
                firstItemInListPublishedDate = firstItemInList.Snippet.PublishedAt;

                // Gather data from various endpoints
                foreach (var searchResult in searchListResponse.Items)
                {
                    switch (searchResult.Id.Kind)
                    {
                        case "youtube#video":
                            videos.Add($"{searchResult.Snippet.Title}");
                            videoId.Add($"{searchResult.Id.VideoId}");
                            thumbnails.Add($"{searchResult.Snippet.Thumbnails.Medium.Url}");
                            publishedAt.Add($"{searchResult.Snippet.PublishedAt}");
                            break;
                        case "youtube#channel":
                            channels.Add($"{searchResult.Snippet.Title}");
                            break;
                    }
                }

                // save images to project folder
                //for (int i = 0; i < videoId.Count; i++)
                //{
                //    try
                //    {
                //        DownloadImage(folder, videoId[i], new Uri(thumbnails[i]));
                //    }
                //    catch (Exception ex)
                //    {
                //        Console.WriteLine($"shits gone wrong: {ex.Message}");
                //    }
                //}

                // add file count to total
                totalFileCount += countOfList;
                Console.WriteLine($"Total Files Downloaded: {totalFileCount}");

                // check the count of the next batch using last item date published
                searchListRequest.PublishedBefore = lastItemInListPublishedDate;
                searchListResponse = await searchListRequest.ExecuteAsync();
                
                countOfList = searchListResponse.Items.Count();

            } while (countOfList > 1);


            // output a count of files downloaded
            Console.WriteLine(String.Format("Channel Name:\n{0}\n", string.Join("\n", channels)));
            Console.WriteLine($"Images saved in {folder}");
        }

        private void DownloadImage(string directoryPath, string fileName, Uri uri)
        {
            var httpClient = new HttpClient();

            // Get the file extension
            var uriWithoutQuery = uri.GetLeftPart(UriPartial.Path);
            var fileExtension = Path.GetExtension(uriWithoutQuery);

            // Create file path and ensure directory exists
            var path = Path.Combine(directoryPath, $"{fileName}{fileExtension}");
            Directory.CreateDirectory(directoryPath);

            // Check file does not already exist
            // todo

            // Download the image and write to the file
            var imageBytes = httpClient.GetByteArrayAsync(uri);
            File.WriteAllBytes(path, imageBytes.Result);
        }
    }
}
