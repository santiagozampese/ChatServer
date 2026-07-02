public class Message
{
    public string? user {get; set;}
    public string? color {get; set;}
    public string? message {get; set;}

    public string GetFormattedMessage()
    {
        string message = this.message ?? "";
        if (!string.IsNullOrEmpty(user))
        {
            string? name="";
            if (User.Name==user) name = "You";
            else name = user;
            message = $"{color}{name}{ConsoleHelper.ResetColor}: {this.message}";         
        }
        
        else
        {
            message = $"{color}{this.message}{ConsoleHelper.ResetColor}";
        }
        return message;
    }
}