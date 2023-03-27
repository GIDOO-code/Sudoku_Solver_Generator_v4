using System;
using System.Collections.Generic;
using System.Linq;

using GIDOO_space;

namespace GNPXcore{
    public partial class SimpleUVWXYZwingGen: AnalyzerBaseV2{
        public List<UCell> FBCX;
		private int stageNoMemo = -9;

        //########################################################## 
        //
        // XYZwing has migrated to ALS version XYZwing.
        //
        //########################################################## 

        public SimpleUVWXYZwingGen( GNPX_AnalyzerMan AnMan ): base(AnMan){
			stageNoMemo = -9;
        }

        public bool XYZwing( ){    return _UVWXYZwing(3); }  //XYZ-wing
        public bool WXYZwing( ){   return _UVWXYZwing(4); }  //WXYZ-wing
        public bool VWXYZwing( ){  return _UVWXYZwing(5); }  //VWXYZ-wing
        public bool UVWXYZwing( ){ return _UVWXYZwing(6); }  //UVWXYZ-wing


        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //7.1..9..8.52...19..8...3.574.3.5.......2.1.......3.7.519.7...3..37...68.8..3..9.1
	    //2.9..3..8.17...63..8...6.259.5.6.......3.2.......9.3.114.6...9..53...48.7..4..2.6

        private bool _UVWXYZwing( int wsz ){     //simple UVWXYZwing
            if( stageNo != stageNoMemo ){
				stageNoMemo = stageNo;
				FBCX = pBOARD.FindAll(p=>p.FreeBC==wsz);
			}
            if( FBCX.Count==0 ) return false;

            bool wingF=false;
            foreach( var P0 in FBCX ){  //focused Cell
                int b0=P0.b;            //focused Block
                foreach( int no in P0.FreeB.IEGet_BtoNo() ){ //focused Digit
                    if( pAnMan.Check_TimeLimit() )  return false;

                    int noB=1<<no;
                    Bit81 B81_P0_conn = (new Bit81(pBOARD,noB,FreeBC:2)) & ConnectedCells[P0.rc];
                    Bit81 B81P0H   = B81_P0_conn&HouseCells[18+P0.b];
                 
                    Bit81 Pout=null, B81P0H2=null;
                    for(int dir=0; dir<2; dir++ ){ //dir 0:row 1:col
                        int h = (dir==0)? P0.r: (9+P0.c);
                        B81P0H2 = B81P0H-HouseCells[h];
                        if( B81P0H2.IsZero() ) continue;
                        Pout = (B81_P0_conn&HouseCells[h])-HouseCells[18+P0.b];
                        if( B81P0H2.Count+Pout.Count != (wsz-1) ) continue;

                        int FreeBin  = B81P0H2.AggregateFreeB(pBOARD);
                        int FreeBout = Pout.AggregateFreeB(pBOARD);
                        if((FreeBin|FreeBout)!=P0.FreeB) continue;
                        Bit81 ELst   = HouseCells[h]&HouseCells[18+P0.b];
                        ELst.BPReset(P0.rc);

                        string msg3="";
                        foreach( var E in ELst.IEGet_rc().Select(p=>pBOARD[p]) ){
                            if( (E.FreeB&noB)>0 ){
                                E.CancelB=noB; wingF=true; 
                                if( SolInfoB ) msg3 += " "+E.rc.ToRCString();
                            }
                        }

                        if(!wingF)  continue;
                        
                        //--- ...wing found -------------
                        SolCode=2;     
                        string[] xyzWingName = { "XYZ-Wing","WXYZ-Wing","VWXYZ-Wing","UVWXYZ-Wing"};
                        string SolMsg = xyzWingName[wsz-3];

                        if( SolInfoB ){
                            P0.Set_CellColorBkgColor_noBit(P0.FreeB,AttCr,SolBkCr2);
                            foreach( var P in B81P0H2.IEGet_rc().Select(p=>pBOARD[p]) ) P.Set_CellColorBkgColor_noBit(P.FreeB,AttCr,SolBkCr);
                            foreach( var P in Pout.IEGet_rc().Select(p=>pBOARD[p]) ) P.Set_CellColorBkgColor_noBit(P.FreeB,AttCr,SolBkCr);

                            string msg0=" Pivot: "+P0.rc.ToRCString();
                            string msg1 = $" in: {B81P0H2.ToString_SameHouseComp()}";
                            string msg2 = $" out: {Pout.ToString_SameHouseComp()}";
                            ResultLong = SolMsg+"\r"+msg0+ "\r   "+msg1+ "\r  "+msg2+ "\r Eliminated: "+msg3.ToString_SameHouseComp();
                            Result = SolMsg+msg0+msg1+msg2;      
                        }
                        if( __SimpleAnalyzerB__ )  return true;
                        if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                        wingF=false;
                    }
                }
            }
            return false;
        }
    }
}