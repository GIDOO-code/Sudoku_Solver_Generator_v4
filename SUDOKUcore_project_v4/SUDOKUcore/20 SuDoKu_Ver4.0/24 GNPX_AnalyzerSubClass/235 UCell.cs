using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using static System.Diagnostics.Debug;
using static System.Math;

using GIDOO_space;
using System;

namespace GNPXcore{
    //Sudoku's cell definition.

    public class UCell{ //Basic Cell Class
        static private readonly Color _Black_ = Colors.Black; 

        static public GNPX_AnalyzerMan pAnMan;
      //public List<UCell>   ppBDL{ get=>pAnMan.pPZL.BOARD; }
      //public object   obj;
        public readonly int  rc;    //cell position(0-80)
        public readonly int  r;     //row
        public readonly int  c;     //column
        public readonly int  b;     //block


        public int      ErrorState; //0:-  1:Fixed 　8:Violation  9:No solution
        public int      No;         //>0:Problem  =0:Open  <0:Solution
        public int      FreeB;      //Bit expression of candidate digits
        public int      FreeBC{ get=>FreeB.BitCount(); }   //Number of candidate digits.

        public int      FixedNo;    //Fixed digit. Obtaine by an algorithm and reflecte to the board status by management processing
        public int      CancelB;    //Digits to exclude(bit expression). Same process as above.

        public List<EColor> ECrLst; //Display color of cell digits. Obtaine by an algorithm.
        public Color    CellBgCr;   //background color. Obtaine by an algorithm
        
//      public bool     Selected;   //(Working variable used in algorithm)    ... Remove in next version
        public int      nx;         //(Working variable used in algorithm)

        public UCell( ){}
        public UCell( int rc, int No=0, int FreeB=0 ){
            this.rc=rc; this.r=rc/9; this.c=rc%9; this.b=rc/27*3+(rc%9)/3;
            this.No=No; this.FreeB=FreeB;
            this.ECrLst=null;
        }

        public void Reset_StepInfo(){
            ErrorState = 0;
            CancelB    = 0;
            FixedNo    = 0;
//          Selected   = false;     ... Remove in next version

            this.ECrLst= null;
            CellBgCr   = _Black_;       
        }

        public UCell Copy( ){
            UCell UCcpy = (UCell)this.MemberwiseClone();
            if( !(UCcpy.ECrLst is null) ){
                UCcpy.ECrLst = new List<EColor>();
                ECrLst.ForEach(p=>UCcpy.ECrLst.Add(new EColor(p)));
            }
            return UCcpy;
        }

        public void Reset_result( bool resetAll=false ){
            CancelB=FixedNo=0; ECrLst=null;
            if( resetAll ) FreeB=0;
        }
        public void Reset_All(){
            CancelB=FixedNo=FreeB=0; ECrLst=null;
            if( No < 0 )  No = 0;
        }

        public void ECrLst_Add( EColor EC ){
            if( ECrLst==null) ECrLst=new List<EColor>();
            if( !ECrLst.Contains(EC) )  ECrLst.Add(EC);

            if( EC.ClrDigitBkg == Color.FromArgb(0xFF,0xFF,0xFF,0x00) )  WriteLine( $"EC:{EC}" );
        }

        //  EColor( Color ClrCellBkg, int noB, Color ClrDigit, Color ClrDigitBkg )
        public void Set_CellBKGColor( Color CellClrBkg ){ 
            ECrLst_Add( new EColor(CellClrBkg,0x1FF,_Black_,_Black_) );
        }



        public void Set_CellDigitsColor_noBit( int noB, Color clr ){
            if( (FreeB&noB) == 0 )  return;
            ECrLst_Add( new EColor( _Black_, noB, clr, _Black_ ) );
        }
        public void Set_CellDigitsColorRev_noBit( int noB, Color clrRev ){
            if( (FreeB&noB) == 0 )  return;
            ECrLst_Add( new EColor( _Black_, noB, _Black_, clrRev) );
        }




        public void Set_CellColorBkgColor_noBit( int noB, Color clr, Color clrBkg ){
            ECrLst_Add( new EColor( clrBkg, noB, clr, _Black_) );
        }
        public void Set_CellDigitsBkgColorRev_noBit( int noB, Color clrRev, Color clrBkg ){
            if( (FreeB&noB) == 0 )  return;
            ECrLst_Add( new EColor( clrBkg, noB, _Black_, clrRev) );
        }









        public string ToStringN(){
            string st2 = $" UCell rc:{rc}[r{r+1}c{c+1}]  no:{No}";
            st2 +=" FreeB:" + FreeB.ToBitStringN(9);
            st2 +=" CancelB:" + CancelB.ToBitStringN(9);
            return st2;
        }
        public string ToStringN2(){
            string st2 = $" UCell {rc.ToRCString()} #{No}";
            st2 +=" FreeB:" + FreeB.ToBitStringN(9);
            st2 +=" CancelB:" + CancelB.ToBitStringN(9);
            return st2;
        }
        public override string ToString(){
            string st2 = $" UCell rc:{rc}[r{r+1}c{c+1}]  no:{No}";
            st2 +=" FreeB:" + FreeB.ToBitString(9);
            st2 +=" FixedNo:" + FixedNo.ToString();
            st2 +=" CancelB:" + CancelB.ToBitString(9);
            return st2;
        }
        public void ResetAnalysisResult(){
            CancelB  = 0;
            FixedNo  = 0;
//            Selected = false;     ... Remove in next version
            this.ECrLst = null;
        }
    }
}