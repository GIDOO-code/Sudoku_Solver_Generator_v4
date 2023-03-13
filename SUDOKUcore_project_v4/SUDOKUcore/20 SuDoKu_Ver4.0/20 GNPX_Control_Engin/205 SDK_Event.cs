using System;
using static System.Diagnostics.Debug;

namespace GNPXcore{
    public delegate void SDKEventHandler( object sender, SDKEventArgs args );
//    public delegate void SDKSolutionEventHandler( object sender, SDKSolutionEventArgs args );  20230307

    public class SDKEventArgs: EventArgs{
	    public string eName;
	    public int    ePara0;
        public int    ePara1;
        public bool   Cancelled;
        public int[]  SDK81;

	    public SDKEventArgs( string eName=null, int ePara0=-1, int ePara1=-1, bool Cancelled=false ){
            try{
		        this.eName = eName;
		        this.ePara0 = ePara0;
                this.ePara1 = ePara1;
                this.Cancelled = Cancelled;
            }
            catch(Exception e ){
                WriteLine(e.Message);
                WriteLine(e.StackTrace);
            }
	    }
        public SDKEventArgs( int[] SDK81 ){
            this.SDK81=SDK81;
        }
    }

#if false
    public class SDKSolutionEventArgs: EventArgs{
        public UProbS   UPB;
        public UPuzzle  GPX;
	    public SDKSolutionEventArgs( UPuzzle  GPX ){
            this.UPB = new UProbS(GPX);
            this.GPX = GPX;
	    }
    }
#endif
}