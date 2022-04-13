namespace viva {

static class DevTools {
    // ----------------------------------------------------------------------------------------
    // Logging
    // ----------------------------------------------------------------------------------------
    private static System.Collections.Generic.Dictionary<string, string> logBuffer = new System.Collections.Generic.Dictionary<string, string>();
    public static void LogExtended(string msg, bool send = false, bool ignoreDoublePosts = false) {
        var st = new System.Diagnostics.StackTrace();
        string src = st.GetFrame(1).GetMethod().Name;
        string buff = (logBuffer.ContainsKey(src) ? logBuffer[src] : "");
        if (msg != "") {
			buff += (buff == "" ? msg : ", " + msg);
            logBuffer[src] = buff;
        }
        if (send && buff != "") {
            if (!ignoreDoublePosts && logBuffer.ContainsKey("dblchk_" + src) && logBuffer["dblchk_" + src] == buff)
            {
                
            } else {
               UnityEngine.Debug.Log("csa," + src + ": " + buff);
            }
            logBuffer["dblchk_" + src] = buff;
            logBuffer[src] = "";
        }
    }
}
}