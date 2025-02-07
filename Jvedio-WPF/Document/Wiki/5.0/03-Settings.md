# 软件设置
# 基本

| 设置                               | 说明                                   |
| :--------------------------------- | :------------------------------------- |
| **青少年模式**                     | 保护青少年健康                         |
| **默认打开上一次关闭的库**         | -                                      |
| **语言**                           | 支持中文、英文、日语                   |
| **详情窗口左右浏览数据库所有影片** | 打开详情窗口后可以浏览该库所有影片     |
| **删除文件同时删除信息**           | 右键删除文件时，同时从数据库中删除信息 |

# 图像

| 设置                               | 说明                               |
| :--------------------------------- | :--------------------------------- |
| **无封面时将已截取的图片作为封面** | 使用影片截图功能后，自动作为封面   |
| **图片自动缩放**                   | 主界面显示的图片自动填充，不留黑边 |

影视图片路径说明如下：

1. 绝对路径：将图片目录放到该路径下
2. 相对路径：相对于 Jvedio.exe 所在的 `/data/username/`目录

3. 相对于影片路径：影片在哪，图片就在那，图片的命名规则如下图所示，./fanart 表示图片名称只要有 fanart 就能识别为海报图

   [<img src="https://s1.ax1x.com/2022/06/11/XcZaqg.png" alt="XcZaqg.png" style="zoom:80%;" />](https://imgtu.com/i/XcZaqg)

对于绝对路径和相对路径，图片的存储目录和图片格式如下：

| 编号 |   文件夹   |   分类   |             说明             |
| :--: | :--------: | :------: | :--------------------------: |
|  1   | Actresses  | 演员头像 |          演员名.jpg          |
|  2   |   BigPic   |  海报图  |      识别码（大写）.jpg      |
|  3   |  ExtraPic  |  预览图  | 识别码（大写）\ 任意图片.jpg |
|  4   |    Gif     |  动态图  |      识别码（大写）.gif      |
|  5   | ScreenShot | 影片截图 | 识别码（大写）\ 任意图片.jpg |
|  6   |  SmallPic  |  缩略图  |      识别码（大写）.jpg      |

5.0 版本支持常见的图片后缀：jpg, jpeg, png, bmp 等，如果有 jpg 则默认使用 jpg

# 扫描与导入


|            设置             |                 说明                 |
| :-------------------------: | :----------------------------------: |
|     **扫描的文件大小**      | 扫描时自动过滤掉小于该文件大小的视频 |
| **同时导入扫描出的NFO文件** | 导入扫描出的NFO，并覆盖已有的信息！  |

软件默认可识别以下几种分段视频


- XXX-001-1, XXX-001-2, XXX-001-3
- XXX-001_1, XXX-001_2, XXX-001_3
- XXX-001cd1, XXX-001cd2, XXX-001cd3
- XXX-001fhd1, XXX-001fhd2, XXX-001fhd3
- XXX-001A, XXX-001B, XXX-001C

勾选【启动时扫描以下目录】后，软件在启动页面会对该目录进行扫描

# 插件

插件更新的地址是 github，如果不显示插件列表，则说明未连接上 github，目前插件仅能下载爬虫插件

# 同步信息

**1. 代理设置**

代理设置有三种模式：

- 不使用代理
- 使用系统代理：一般来说，代理软件开启后，会应用于系统代理
- 自定义代理：配置自定义代理，支持 HTTP 和 SOCKS 代理

**2. 刮削器列表**

对于下载的插件，会显示在刮削器列表中，刮削器不会自带网址，需要自行填入，并且需要填入 Headers！

测试通过后该刮削器才能用来刮削信息

刮削器可刮削的视频类型在插件中查看


# 显示

无

# 主题

目前仅支持白/黑两款皮肤，后面更新会加上更多皮肤

# 翻译

暂无


# 人工智能

暂无

# 视频处理

前往 [微云](https://share.weiyun.com/QDVHNbfJ)  下载 ffmpeg.exe，添加该路径到软件中，或前往[FFMPEG官网](https://github.com/BtbN/FFmpeg-Builds/releases)下载对应压缩包并解压

设置截图数目后，影片右键-截图，即可在 `Pic\ScreenShot\XXX-001` 中生成影片截图

注意，如果设置了跳过开头/结尾，导致视频帧数小于总帧数，则会截图视频

# 自定外观

暂无

# 重命名

需要配置重命名后，才能在右键点击重命名

# 库

资源存在索引：筛选影片的时候用于筛选出可播放/不可播放的影视资源

图片存在索引：筛选出图片存在/不存在的影视资源

