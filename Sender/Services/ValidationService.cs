using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Sender.Services
{
    interface IValidationService
    {

    }

    public class ValidationService : IValidationService
    {
        private const string _regexIpAddress = @"^\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b$";

        private const string _regexPort = @"^[1-9]{1}\d{0,4}$";

        public static bool IpAddressValidation(string strIpAddress)
        {
            var regExp = new Regex(_regexIpAddress);

            var IsIpAddress = regExp.IsMatch(strIpAddress);

            return IsIpAddress;
        }

        public static bool PortValidation(string strPortConnection)
        {
            var regExp = new Regex(_regexPort);

            var IsPort = regExp.IsMatch(strPortConnection);

            return IsPort;
        }
    }
}