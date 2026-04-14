namespace MMN.Web.Models;

public class UserFacingException(string message, Exception? innerException = null) : Exception(message, innerException);
