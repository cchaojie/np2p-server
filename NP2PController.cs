using System.Linq;
using System.Threading.Tasks;
using CoreCms.Net.Auth.HttpContextUser;
using CoreCms.Net.IServices;
using CoreCms.Net.Model.FromBody;
using CoreCms.Net.Model.ViewModels.UI;
using Microsoft.AspNetCore.Mvc;
using SqlSugar;
using Dapper;
using System.Data;
using System.Data.SqlClient;
using System;
using Microsoft.AspNetCore.Http;
using System.IO;
using static SKIT.FlurlHttpClient.Wechat.Api.Models.ProductLimitedDiscountGetListResponse.Types;
using static SKIT.FlurlHttpClient.Wechat.Api.Models.WxaGetWxaGameFrameResponse.Types.Data.Types;
using System.Diagnostics;

namespace CoreCms.Net.Web.WebApi.Controllers
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class NP2PController : ControllerBase
    {

        private IHttpContextUser _user;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="user"></param>
        public NP2PController(IHttpContextUser user
            , IHttpContextAccessor httpContextAccessor)
        {
            _user = user;
            _httpContextAccessor = httpContextAccessor;
        }

        public class ThreejsPoseFrame
        {
            public int FrameId { get; set; }
            public Guid? ThreejsPoseTaskId { get; set; }
            public string FrameImage { get; set; }
            public string FrameStatus { get; set; }
            public DateTime? CreateDate { get; set; }
            public DateTime? GenerateDate { get; set; }
            public int? FrameIndex { get; set; }
            public string ResultImage { get; set; }
            public string Prompt { get; set; }
            public string Seed { get; set; }
            public DateTime? PaintDate { get; set; }
        }

        public class ThreejsPoseTask
        {
            public Guid ThreejsPoseTaskId { get; set; }
            public string TaskStatus { get; set; }
            public string VideoPath { get; set; }
            public DateTime? CreateDate { get; set; }
            public DateTime? GenerateDate { get; set; }
        }

        #region 获取Frame列表
        /// <summary>
        /// 获取Frame列表
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> FrameList(Guid taskId)
        {
            var jm = new WebApiCallBack();

            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            var list = con.Query<ThreejsPoseFrame>("SELECT * FROM ThreejsPoseFrame WHERE ThreejsPoseTaskId=@ThreejsPoseTaskId", new { ThreejsPoseTaskId = taskId });
            con.Close();
            jm.status = true;
            jm.data = list;

            return jm;
        }

        #endregion

        #region 获取New Frame
        /// <summary>
        /// 获取New Frame
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> NewFrame()
        {
            var jm = new WebApiCallBack();

            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            var frame = con.Query<ThreejsPoseFrame>("SELECT TOP 1 * FROM ThreejsPoseFrame WHERE FrameStatus='NEW' OR (FrameStatus='INPROCESS' AND DATEADD(MINUTE,1,PaintDate)<GETDATE()) ORDER BY CreateDate ASC").FirstOrDefault();
            if (frame != null)
                con.Execute("UPDATE ThreejsPoseFrame SET FrameStatus='INPROCESS',PaintDate=GETDATE() WHERE FrameId=@FrameId", frame);
            con.Close();
            jm.status = true;
            jm.data = frame;

            if (frame == null) jm.status = false;

            return jm;
        }

        #endregion

        #region 创建Task
        /// <summary>
        /// 创建Task
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> NewTask([FromBody] ThreejsPoseTask entity)
        {
            var jm = new WebApiCallBack();

            entity.TaskStatus = "NEW";
            entity.VideoPath = "";
            entity.CreateDate = DateTime.Now;
            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            con.Execute("INSERT INTO ThreejsPoseTask(ThreejsPoseTaskId,TaskStatus,VideoPath,CreateDate)VALUES(@ThreejsPoseTaskId,@TaskStatus,@VideoPath,@CreateDate)", entity);
            con.Close();
            jm.status = true;
            jm.data = entity.ThreejsPoseTaskId;

            return jm;
        }

        #endregion

        #region 上传Video
        /// <summary>
        /// 上传Video
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> UploadVideo(string taskid)
        {
            var jm = new WebApiCallBack();
            var path = @"C:\wwwroot\np2p\" + taskid + "\result.mp4";

            var files = _httpContextAccessor.HttpContext.Request.Form.Files;
            if (files.Count() > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await files[0].CopyToAsync(memoryStream);

                    System.IO.File.WriteAllBytes(path, memoryStream.ToArray());
                }
            }

            ThreejsPoseTask entity = new ThreejsPoseTask()
            {
                ThreejsPoseTaskId = new Guid(taskid),
                TaskStatus = "COMPLETED",
                VideoPath = path,
                GenerateDate = DateTime.Now
            };
            entity.TaskStatus = "NEW";
            entity.VideoPath = path;
            entity.GenerateDate = DateTime.Now;
            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            con.Execute("UPDATE ThreejsPoseTask SET TaskStatus=@TaskStatus,VideoPath=@VideoPath,GenerateDate=@GenerateDate WHERE ThreejsPoseTaskId=@ThreejsPoseTaskId", entity);
            con.Close();
            jm.status = true;
            jm.data = entity.ThreejsPoseTaskId;

            return jm;
        }

        #endregion

        #region 上传图片
        /// <summary>
        /// 上传图片
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> UploadImage(int frameId)
        {
            var jm = new WebApiCallBack();

            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            var frame = con.Query<ThreejsPoseFrame>("SELECT TOP 1 * FROM ThreejsPoseFrame WHERE FrameStatus='NEW' ORDER BY CreateDate ASC", new ThreejsPoseFrame { FrameId = frameId }).FirstOrDefault();

            if (frame != null)
            {
                var path = @"C:\wwwroot\np2p\" + frame.ThreejsPoseTaskId + @"\" + frame.FrameIndex + ".png";

                var files = _httpContextAccessor.HttpContext.Request.Form.Files;
                if (files.Count() > 0)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await files[0].CopyToAsync(memoryStream);

                        System.IO.File.WriteAllBytes(path, memoryStream.ToArray());
                    }
                }

                jm.status = true;
                jm.data = frame.FrameId;
            }

            con.Close();

            return jm;
        }

        #endregion

        #region 获取Frame
        /// <summary>
        /// 获取Frame
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> GetFrame(int frameId)
        {
            var jm = new WebApiCallBack();

            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            var frame = con.Query<ThreejsPoseFrame>("SELECT TOP 1 * FROM ThreejsPoseFrame WHERE FrameStatus='NEW' ORDER BY CreateDate ASC", new ThreejsPoseFrame { FrameId = frameId }).FirstOrDefault();
            con.Close();

            if (frame != null)
            {
                jm.status = true;
                jm.data = frame;
            }

            return jm;
        }

        #endregion

        #region 创建Frame
        /// <summary>
        /// 创建Frame
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> CreateFrame([FromBody] ThreejsPoseFrame entity)
        {
            var jm = new WebApiCallBack();

            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            var frameId = con.ExecuteScalar<int>(@"
INSERT INTO ThreejsPoseFrame(
       ThreejsPoseTaskId
      ,FrameImage
      ,FrameStatus
      ,CreateDate
      ,FrameIndex
      ,Prompt
      ,Seed
)VALUES(
       @ThreejsPoseTaskId
      ,@FrameImage
      ,'NEW'
      ,GETDATE()
      ,@FrameIndex
      ,@Prompt
      ,@Seed
);SELECT @@IDENTITY;", entity);
            con.Close();

            jm.status = true;
            jm.data = frameId;

            return jm;
        }

        #endregion

        #region 上传图片
        /// <summary>
        /// 上传图片
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> UploadImage64([FromBody] ThreejsPoseFrame entity)
        {
            var jm = new WebApiCallBack();

            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            var frame = con.Query<ThreejsPoseFrame>("UPDATE ThreejsPoseFrame SET ResultImage=@ResultImage,GenerateDate=GETDATE(),FrameStatus='COMPLETED' WHERE FrameId=@FrameId;SELECT * FROM ThreejsPoseFrame WHERE FrameId=@FrameId;", entity).FirstOrDefault();

            if (!Directory.Exists(@"C:\wwwroot\np2p\" + frame.ThreejsPoseTaskId.Value.ToString()))
            {
                Directory.CreateDirectory(@"C:\wwwroot\np2p\" + frame.ThreejsPoseTaskId.Value.ToString());
            }

            var path = @"C:\wwwroot\np2p\" + frame.ThreejsPoseTaskId.Value.ToString() + @"\" + frame.FrameIndex + ".png";
            System.IO.File.WriteAllBytes(path, Convert.FromBase64String(frame.ResultImage));

            if (con.ExecuteScalar<int>("SELECT COUNT(*) FROM ThreejsPoseFrame WHERE ThreejsPoseTaskId=@ThreejsPoseTaskId AND FrameStatus<>'COMPLETED'", frame) == 0)
            {
                using (Process p = new Process())
                {
                    string output = "";

                    p.StartInfo.WorkingDirectory = @"C:\wwwroot\np2p\" + frame.ThreejsPoseTaskId.Value.ToString();
                    p.StartInfo.FileName = @"C:\Users\Administrator\ffmpeg\bin\ffmpeg.exe";//可执行程序路径
                    p.StartInfo.Arguments = "  -r 10 -f image2 -start_number 0 -i %d.png -vcodec libx264 -crf 25  -pix_fmt yuv420p result.mp4";//参数以空格分隔，如果某个参数为空，可以传入""
                    p.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
                    p.StartInfo.CreateNoWindow = true;//不显示程序窗口
                    //p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                    //p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                    //p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                    p.Start();
                    p.WaitForExit();
                    //正常运行结束放回代码为0
                    if (p.ExitCode != 0)
                    {
                        //output = p.StandardError.ReadToEnd();
                        //output = output.ToString().Replace(System.Environment.NewLine, string.Empty);
                        //output = output.ToString().Replace("\n", string.Empty);
                        //throw new Exception(output.ToString());
                    }
                    else
                    {
                        //output = p.StandardOutput.ReadToEnd();
                    }
                }

                path = @"C:\wwwroot\np2p\" + frame.ThreejsPoseTaskId.Value.ToString() + @"\result.mp4";

                con.Execute("UPDATE ThreejsPoseTask SET TaskStatus='COMPLETED',VideoPath=@VideoPath,GenerateDate=GETDATE() WHERE ThreejsPoseTaskId=@ThreejsPoseTaskId", new ThreejsPoseTask()
                {
                    ThreejsPoseTaskId = frame.ThreejsPoseTaskId.Value,
                    VideoPath = path
                });
            }
            con.Close();

            jm.status = true;

            return jm;
        }

        #endregion

        #region 获取Task
        /// <summary>
        /// 获取Task
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public async Task<WebApiCallBack> GetTask(string taskId)
        {
            var jm = new WebApiCallBack();

            if (!Directory.Exists(@"C:\wwwroot\np2p\" + taskId))
            {
                Directory.CreateDirectory(@"C:\wwwroot\np2p\" + taskId);
            }

            IDbConnection con = new SqlConnection(Configuration.AppSettingsConstVars.DbSqlConnection);
            if (con.ExecuteScalar<int>("SELECT COUNT(*) FROM ThreejsPoseFrame WHERE ThreejsPoseTaskId=@ThreejsPoseTaskId AND FrameStatus<>'COMPLETED'", new { ThreejsPoseTaskId =new Guid(taskId)}) == 0)
            {
                using (Process p = new Process())
                {
                    string output = "";

                    p.StartInfo.WorkingDirectory = @"C:\wwwroot\np2p\" + taskId;
                    p.StartInfo.FileName = @"C:\Users\Administrator\ffmpeg\bin\ffmpeg.exe";//可执行程序路径
                    p.StartInfo.Arguments = " -y -r 10 -f image2 -start_number 0 -i %d.png -vcodec libx264 -crf 25  -pix_fmt yuv420p result.mp4";//参数以空格分隔，如果某个参数为空，可以传入""
                    p.StartInfo.UseShellExecute = false;//是否使用操作系统shell启动
                    p.StartInfo.CreateNoWindow = true;//不显示程序窗口
                    //p.StartInfo.RedirectStandardOutput = true;//由调用程序获取输出信息
                    //p.StartInfo.RedirectStandardInput = true;   //接受来自调用程序的输入信息
                    //p.StartInfo.RedirectStandardError = true;   //重定向标准错误输出
                    p.Start();
                    p.WaitForExit();
                    //正常运行结束放回代码为0
                    if (p.ExitCode != 0)
                    {
                        //output = p.StandardError.ReadToEnd();
                        //output = output.ToString().Replace(System.Environment.NewLine, string.Empty);
                        //output = output.ToString().Replace("\n", string.Empty);
                        //throw new Exception(output.ToString());
                    }
                    else
                    {
                        //output = p.StandardOutput.ReadToEnd();
                    }
                }

                var path = @"C:\wwwroot\np2p\" + taskId + @"\result.mp4";

                con.Execute("UPDATE ThreejsPoseTask SET TaskStatus='COMPLETED',VideoPath=@VideoPath,GenerateDate=GETDATE() WHERE ThreejsPoseTaskId=@ThreejsPoseTaskId", new ThreejsPoseTask()
                {
                    ThreejsPoseTaskId = new Guid(taskId),
                    VideoPath = path
                });

                jm.status = true;
            }

            return jm;
        }

        #endregion
    }
}
