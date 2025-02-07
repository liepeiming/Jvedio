﻿using Jvedio.CommonNet.Entity;
using Jvedio.Core.CustomEventArgs;
using Jvedio.Core.CustomTask;
using Jvedio.Core.Enums;
using Jvedio.Core.Exceptions;
using Jvedio.Entity;
using SuperControls.Style;
using SuperUtils.Common;
using SuperUtils.Framework.ORM.Attributes;
using SuperUtils.Framework.ORM.Wrapper;
using SuperUtils.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static Jvedio.MapperManager;

namespace Jvedio.Core.Net
{
    // todo 检视
    public class DownLoadTask : AbstractTask
    {
        public bool DownloadPreview { get; set; } // 是否下载预览图

        public event EventHandler onDownloadSuccess;

        public event EventHandler onDownloadPreview;

        private static class Delay
        {
            public static int INFO = 1000;
            public static int EXTRA_IMAGE = 500;
            public static int BIG_IMAGE = 50;
            public static int SMALL_IMAGE = 50;
        }

        public DownLoadTask(Video video, bool downloadPreview = false, bool overrideInfo = false) : this(video.toMetaData())
        {
            Title = string.IsNullOrEmpty(video.VID) ? video.Title : video.VID;
            DownloadPreview = downloadPreview;
            OverrideInfo = overrideInfo;
        }

        static DownLoadTask()
        {
            STATUS_TO_TEXT_DICT[TaskStatus.Running] = $"{LangManager.GetValueByKey("Downloading")}...";
        }

        public DownLoadTask(MetaData data) : base()
        {
            DataID = data.DataID;
            DataType = data.DataType;
        }

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj is DownLoadTask other)
                return other.DataID.Equals(DataID);
            return false;
        }

        public override int GetHashCode()
        {
            return DataID.GetHashCode();
        }

        public long DataID { get; set; }

        public DataType DataType { get; set; }

        public string Title { get; set; }

        public bool OverrideInfo { get; set; }// 强制下载覆盖信息

        public override void DoWork()
        {
            Task.Run(async () =>
            {
                Progress = 0;
                stopwatch.Start();
                Dictionary<string, object> dict = null;
                if (DataType == DataType.Video)
                {
                    Video video = videoMapper.SelectVideoByID(DataID);
                    if (video == null || video.DataID <= 0)
                    {
                        Message = $"不存在 DataID={DataID} 的资源";
                        FinalizeWithCancel();
                        return;
                    }

                    VideoDownLoader downLoader = new VideoDownLoader(video, token);
                    RequestHeader header = null;

                    // 判断是否需要下载，自动跳过已下载的信息
                    if (video.toDownload() || OverrideInfo)
                    {
                        StatusText = LangManager.GetValueByKey("DownloadInfo");
                        if (!string.IsNullOrEmpty(video.VID))
                        {
                            // 有 VID 的
                            try
                            {
                                dict = await downLoader.GetInfo((h) => { header = h; });
                            }
                            catch (CrawlerNotFoundException ex)
                            {
                                // todo 显示到界面上
                                Message = ex.Message;
                                logger.Error(Message);
                                FinalizeWithCancel();
                                return;
                            }
                            catch (DllLoadFailedException ex)
                            {
                                Message = ex.Message;
                                logger.Error(Message);
                                FinalizeWithCancel();
                                return;
                            }
                        }
                        else
                        {
                            // 无 VID 的
                            Status = TaskStatus.Canceled;
                        }

                        // 等待了很久都没成功
                        await Task.Delay(Delay.INFO);
                    }
                    else
                    {
                        logger.Info(LangManager.GetValueByKey("SkipDownLoadInfoAndDownloadImage"));
                    }

                    bool success = true; // 是否刮削到信息（包括db的部分信息）
                    Progress = 33f;
                    if (dict != null && dict.ContainsKey("Error"))
                    {
                        string error = dict["Error"].ToString();
                        if (!string.IsNullOrEmpty(error))
                        {
                            Message = error;
                            logger.Error(error);
                        }

                        success = dict.ContainsKey("Title") && !string.IsNullOrEmpty(dict["Title"].ToString());
                    }

                    if (!success)
                    {
                        dict = null;

                        // 发生了错误，停止下载
                        FinalizeWithCancel();

                        // 但是已经请求了网址，所以视为完成，并加入到长时间等待队列
                        Status = TaskStatus.RanToCompletion;
                        return;
                    }

                    bool downloadInfo = video.parseDictInfo(dict); // 是否从网络上刮削了信息
                    if (downloadInfo)
                    {
                        logger.Info(LangManager.GetValueByKey("SaveToLibrary"));

                        // 并发锁
                        videoMapper.UpdateById(video);
                        metaDataMapper.UpdateById(video.toMetaData());

                        // 保存 dataCode
                        if (dict.ContainsKey("DataCode") && dict.ContainsKey("WebType"))
                        {
                            UrlCode urlCode = new UrlCode();
                            urlCode.LocalValue = video.VID;
                            urlCode.RemoteValue = dict["DataCode"].ToString();
                            urlCode.ValueType = "video";
                            urlCode.WebType = dict["WebType"].ToString();
                            urlCodeMapper.Insert(urlCode, InsertMode.Replace);
                        }

                        // 保存 nfo
                        video.SaveNfo();

                        onDownloadSuccess?.Invoke(this, null);
                    }
                    else
                    {
                        dict = null;
                    }

                    if (Canceld)
                    {
                        FinalizeWithCancel();
                        return;
                    }

                    // 可能刮削了信息，但是没刮削图片
                    if (header == null)
                    {
                        header = new RequestHeader();
                        header.WebProxy = ConfigManager.ProxyConfig.GetWebProxy();
                        header.TimeOut = ConfigManager.ProxyConfig.HttpTimeout * 1000; // 转为 ms
                    }

                    object o = getInfoFromExist("BigImageUrl", video, dict);
                    string imageUrl = o != null ? o.ToString() : string.Empty;
                    StatusText = LangManager.GetValueByKey("Poster");

                    // 1. 大图
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        // todo 原来的 domain 可能没法用，得替换 domain
                        string saveFileName = video.getBigImage(Path.GetExtension(imageUrl), false);
                        if (!File.Exists(saveFileName))
                        {
                            byte[] fileByte = await downLoader.DownloadImage(imageUrl, header, (error) =>
                             {
                                 if (!string.IsNullOrEmpty(error))
                                     logger.Error($"{imageUrl} => {error}");
                             });
                            if (fileByte != null && fileByte.Length > 0)
                                FileHelper.ByteArrayToFile(fileByte, saveFileName);
                            await Task.Delay(Delay.BIG_IMAGE);
                        }
                        else
                        {
                            logger.Info($"{LangManager.GetValueByKey("SkipDownloadImage")} {saveFileName}");
                        }
                    }

                    Progress = 66f;

                    if (Canceld)
                    {
                        FinalizeWithCancel();
                        return;
                    }

                    StatusText = LangManager.GetValueByKey("Thumbnail");
                    o = getInfoFromExist("SmallImageUrl", video, dict);
                    imageUrl = o != null ? o.ToString() : string.Empty;

                    // 2. 小图
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        string saveFileName = video.getSmallImage(Path.GetExtension(imageUrl), false);
                        if (!File.Exists(saveFileName))
                        {
                            byte[] fileByte = await downLoader.DownloadImage(imageUrl, header, (error) =>
                            {
                                if (!string.IsNullOrEmpty(error))
                                    logger.Error($"{imageUrl} => {error}");
                            });
                            if (fileByte != null && fileByte.Length > 0)
                                FileHelper.ByteArrayToFile(fileByte, saveFileName);
                            await Task.Delay(Delay.SMALL_IMAGE);
                        }
                        else
                        {
                            logger.Info($"{LangManager.GetValueByKey("SkipDownloadImage")} {saveFileName}");
                        }
                    }

                    Progress = 77f;
                    if (Canceld)
                    {
                        FinalizeWithCancel();
                        return;
                    }

                    onDownloadSuccess?.Invoke(this, null);
                    StatusText = LangManager.GetValueByKey("Actors");

                    object names = getInfoFromExist("ActorNames", video, dict);
                    object urls = getInfoFromExist("ActressImageUrl", video, dict);

                    // 3. 演员信息和头像
                    if (names != null && urls != null && names is List<string> actorNames && urls is List<string> ActressImageUrl)
                    {
                        if (actorNames != null && ActressImageUrl != null && actorNames.Count == ActressImageUrl.Count)
                        {
                            for (int i = 0; i < actorNames.Count; i++)
                            {
                                string actorName = actorNames[i];
                                string url = ActressImageUrl[i];
                                ActorInfo actorInfo = actorMapper.SelectOne(new SelectWrapper<ActorInfo>().Eq("ActorName", actorName));
                                if (actorInfo == null || actorInfo.ActorID <= 0)
                                {
                                    actorInfo = new ActorInfo();
                                    actorInfo.ActorName = actorName;
                                    actorInfo.ImageUrl = url;
                                    actorMapper.Insert(actorInfo);
                                }

                                // 保存信息
                                string sql = $"insert or ignore into metadata_to_actor (ActorID,DataID) values ({actorInfo.ActorID},{video.DataID})";
                                metaDataMapper.ExecuteNonQuery(sql);

                                // 下载图片
                                string saveFileName = actorInfo.GetImagePath(video.Path, Path.GetExtension(url), false);
                                if (!File.Exists(saveFileName))
                                {
                                    byte[] fileByte = await downLoader.DownloadImage(url, header, (error) =>
                                    {
                                        if (!string.IsNullOrEmpty(error))
                                            logger.Error($"{url} => {error}");
                                    });
                                    if (fileByte != null && fileByte.Length > 0)
                                        FileHelper.ByteArrayToFile(fileByte, saveFileName);
                                }
                                else
                                {
                                    logger.Info($"{LangManager.GetValueByKey("SkipDownloadImage")} {saveFileName}");
                                }
                            }
                        }
                    }

                    Progress = 88f;
                    if (Canceld)
                    {
                        FinalizeWithCancel();
                        return;
                    }

                    // 4. 下载预览图
                    urls = getInfoFromExist("ExtraImageUrl", video, dict);
                    if (DownloadPreview && urls != null && urls is List<string> imageUrls)
                    {
                        StatusText = LangManager.GetValueByKey("Preview");
                        if (imageUrls != null && imageUrls.Count > 0)
                        {
                            for (int i = 0; i < imageUrls.Count; i++)
                            {
                                if (Canceld)
                                {
                                    FinalizeWithCancel();
                                    return;
                                }

                                string url = imageUrls[i];

                                // 下载图片
                                string saveFiledir = video.getExtraImage();
                                if (!Directory.Exists(saveFiledir)) Directory.CreateDirectory(saveFiledir);
                                string saveFileName = Path.Combine(saveFiledir, Path.GetFileName(url));
                                if (!File.Exists(saveFileName))
                                {
                                    StatusText = $"{LangManager.GetValueByKey("Preview")} {i + 1}/{imageUrls.Count}";
                                    byte[] fileByte = await downLoader.DownloadImage(url, header, (error) =>
                                    {
                                        if (!string.IsNullOrEmpty(error))
                                            logger.Error($"{url} => {error}");
                                    });
                                    if (fileByte != null && fileByte.Length > 0)
                                    {
                                        FileHelper.ByteArrayToFile(fileByte, saveFileName);
                                        PreviewImageEventArgs arg = new PreviewImageEventArgs(saveFileName, fileByte);
                                        onDownloadPreview?.Invoke(this, arg);
                                    }

                                    await Task.Delay(Delay.EXTRA_IMAGE);
                                }
                                else
                                {
                                    logger.Info($"{LangManager.GetValueByKey("SkipDownloadImage")} {saveFileName}");
                                }
                            }
                        }
                    }
                    else
                    if (!DownloadPreview)
                        logger.Info(LangManager.GetValueByKey("NotSetPreviewDownload"));
                    Success = true;
                    Status = TaskStatus.RanToCompletion;
                }

                Console.WriteLine("下载完成！");
                Running = false;
                Progress = 100.00f;
                stopwatch.Stop();
                ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                logger.Info($"{LangManager.GetValueByKey("TotalCost")} {ElapsedMilliseconds} ms");
            });
        }

        private object getInfoFromExist(string type, Video video, Dictionary<string, object> dict)
        {
            if (dict != null && dict.Count > 0)
            {
                if (dict.ContainsKey(type))
                {
                    if (dict[type].GetType() == typeof(Newtonsoft.Json.Linq.JArray))
                    {
                        Newtonsoft.Json.Linq.JArray jArray = Newtonsoft.Json.Linq.JArray.Parse(dict[type].ToString());
                        return jArray.Select(x => x.ToString()).ToList();
                    }

                    return dict[type];
                }

                return null;
            }
            else if (video != null)
            {
                string imageUrls = video.ImageUrls;
                if (!string.IsNullOrEmpty(imageUrls))
                {
                    Dictionary<string, object> dic = JsonUtils.TryDeserializeObject<Dictionary<string, object>>(imageUrls);
                    if (dic == null) return null;
                    return getInfoFromExist(type, null, dic); // 递归调用
                }
            }

            return null;
        }
    }
}
