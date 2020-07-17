using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Net;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors;
using Arad.ChunkedUpload.Service;
using Arad.ChunkedUpload.Data;
using Arad.ChunkedUpload.Model;


namespace Arad.ChunkedUpload.Controllers
{
    /// <summary>
    /// File Management Controller
    /// </summary>
    /// <remarks>Manages upload sessions</remarks>
    [Route("api/file")]
    [EnableCors("MyPolicy")]
    public class FileController : Controller
    {
        private static UploadService uploadService = new UploadService(new LocalFileSystemRepository());

        public FileController()
        {

        }

        /// <summary>
        /// Create an upload session
        /// </summary>
        /// <remarks>creates a new upload session</remarks>
        /// <param name="userId">User ID</param>
        /// <param name="sessionParams">Session creation params</param>
        [HttpPost("create/{userId}")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public SessionCreationStatusResponse StartSession([FromRoute] long userId,
                         [FromForm] CreateSessionParams sessionParams)
        {

            Session session = uploadService.createSession(userId, sessionParams.FileName,
                                                          sessionParams.ChunkSize.Value,
                                                          sessionParams.TotalSize.Value);

            return SessionCreationStatusResponse.fromSession(session);
        }

        /// <summary>
        /// Uploads a file chunk
        /// </summary>
        /// <remarks>uploads a file chunk</remarks>
        /// <param name="userId">User ID</param>
        /// <param name="sessionId">Session ID</param>
        /// <param name="chunkNumber">Chunk number (starts from 1)</param>
        /// <param name="inputFile">File chunk content</param>
        [HttpPut("upload/user/{userId}/session/{sessionId}/")]
        [Produces("application/json")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public JsonResult UploadFileChunk([FromRoute, Required] long? userId,
                                        [FromRoute, Required] string sessionId,
                                        [FromQuery, Required] int? chunkNumber,
                                        [FromForm] IFormFile inputFile)
        {
            if (!userId.HasValue)
                return badRequest("User missing");

            if (String.IsNullOrWhiteSpace(sessionId))
                return badRequest("Session ID is missing");

            if (chunkNumber < 1)
                return badRequest("Invalid chunk number");

            IFormFile file = (inputFile ?? Request.Form.Files.First());

            uploadService.persistBlock(sessionId, userId.Value, chunkNumber.Value, ToByteArray(file.OpenReadStream()));

            return Json("Ok");
        }

        /// <summary>
        /// Gets the status of a single upload
        /// </summary>
        /// <remarks>gets the status of a single upload</remarks>
        /// <param name="sessionId">Session ID</param>
        [HttpGet("upload/{sessionId}")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public UploadStatusResponse GetUploadStatus([FromRoute, Required] string sessionId)
        {
            return UploadStatusResponse.fromSession(uploadService.getSession(sessionId));
        }

        /// <summary>
        /// Gets the status of all uploads
        /// </summary>
        /// <remarks>gets the status of all uploads</remarks>
        [HttpGet("uploads")]
        [Produces("application/json")]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public List<UploadStatusResponse> GetAllUploadStatus()
        {
            return UploadStatusResponse.fromSessionList(uploadService.getAllSessions());
        }

        /// <summary>
        /// Downloads a previously uploaded file
        /// </summary>
        /// <param name="sessionId">Session ID</param>
        /// <remarks>downloads a previously uploaded file</remarks>
        [HttpGet("download/{sessionId}")]
        [Produces("multipart/form-data")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public void DownloadFile([FromRoute, Required] string sessionId)
        {
            Session session = uploadService.getSession(sessionId);

            var response = targetResponse ?? Response;

            response.ContentType = "application/octet-stream";
            response.ContentLength = session.FileInfo.FileSize;
            response.Headers["Content-Disposition"] = "attachment; fileName=" + session.FileInfo.FileName;

            uploadService.WriteToStream(targetOutputStream ?? Response.Body, session);
        }

        private byte[] ToByteArray(Stream stream)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private JsonResult badRequest(string message) {
            var result = new JsonResult("{'message': '" + message + "' }");
            result.StatusCode = 400;
            return result;
        }

        Stream targetOutputStream = null;
        [ApiExplorerSettings(IgnoreApi=true)]
        public void SetOuputStream(Stream replacementStream) {
            this.targetOutputStream = replacementStream;
        }

        HttpResponse targetResponse = null;
        [ApiExplorerSettings(IgnoreApi=true)]
        public void SetTargetResponse(HttpResponse replacementResponse) {
            this.targetResponse = replacementResponse;
        }
    }
}
