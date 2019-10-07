using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LightwaveRFLinkPlusSharp
{
    /// <summary>
    /// Thrown when all attempts to get a new access token have failed. For details on how to pass in a seed refresh token, see the summary on the LightwaveAPI 
    /// constructor. For debugging purposes, a record of the events leading to this failure can be found in this exception's TokenRequestLog property.
    /// </summary>
    public class InvalidRefreshTokenException : Exception
    {
        public string TokenRequestLog { get; set; }

        public InvalidRefreshTokenException(string tokenRequestLog)
            : base("All attempts to get a new access token have failed. For details on how to pass in a seed refresh token, see the "
                 + "summary on the LightwaveAPI constructor. For debugging purposes, a record of the events leading to this failure "
                 + "can be found in this exception's TokenRequestLog property")
        {
            TokenRequestLog = tokenRequestLog;
        }
    }

    /// <summary>
    /// Thrown when the web API call returns an unsuccessful status
    /// </summary>
    public class LightwaveAPIRequestException : Exception
    {
        public HttpStatusCode StatusCode { get; }

        public LightwaveAPIRequestException(string message, HttpStatusCode statusCode)
            : base(message)
        {
            StatusCode = statusCode;
        }
    }

    /// <summary>
    /// Thrown when the Json received from the web API call can not be parsed as expected
    /// </summary>
    public class UnexpectedJsonException : Exception
    {
        public string Json { get; }

        public UnexpectedJsonException(string message, string json)
            : base(message)
        {
            Json = json;
        }
    }

    /// <summary>
    /// Thrown when no Structures can be found in your LinkPlus ecosystem
    /// </summary>
    public class NoStructuresFoundException : Exception
    {
    }

    /// <summary>
    /// Thrown when the specified Structure cannot be found
    /// </summary>
    public class StructureNotFoundException : Exception
    {
        public string StructureId { get; }

        public StructureNotFoundException(string structureId)
        {
            StructureId = structureId;
        }
    }

    /// <summary>
    /// Thrown when the specified Feature cannot be found
    /// </summary>
    public class FeatureNotFoundException : Exception
    {
        public string FeatureId { get; }

        public FeatureNotFoundException(string featureId)
        {
            FeatureId = featureId;
        }
    }
}
