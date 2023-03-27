using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using static System.Diagnostics.Debug;

using GIDOO_space;

namespace GNPXcore {
    public partial class AnalyzerBaseV2{
        static public bool   __SimpleAnalyzerB__;
        static public int    __DebugBreak;
        private const int    S=1, W=2;

        private GNPZ_Engin      pGNPX_Eng;
        public GNPX_AnalyzerMan pAnMan;

        public  int             stageNo => pGNPX_Eng.PZLMan.stageNo;
        public  UPuzzle         pPZL{ get=>pGNPX_Eng.PZLMan.PZL; set=>pGNPX_Eng.PZLMan.PZL=value; }
        public List<UCell>      pBOARD{ get=>pPZL.BOARD; set=>pPZL.BOARD=value; }
        public bool             SolInfoB{ get=>GNPZ_Engin.SolInfoB; set=>GNPZ_Engin.SolInfoB=value; }
        public int              SolCode{  get=>pPZL.SolCode; set=>pPZL.SolCode=value; }
        public string           Result{   get=>pPZL.Sol_Result; set=>pPZL.Sol_Result=value; }
        public string           ResultLong{ get=>pPZL.Sol_ResultLong; set=>pPZL.Sol_ResultLong=value; }

        public bool             chbConfirmMultipleCells{ get=> pAnMan.chbConfirmMultipleCells; } 


		public string		 extResult{
                                set{
                                    if( value=="" ) pBOARD.ForEach( P => P.Reset_result() );
                                    pPZL.extResult=value; 
                                }
                                get=> pPZL.extResult; }


 #if RegularVersion       
		public SuperLinkMan	 pSprLKsMan{ get=>pAnMan.SprLKsMan; }
        public CellLinkMan   CeLKMan{    get=>pAnMan.SprLKsMan.CeLKMan; }
        public ALSLinkMan    ALSMan{     get=>pAnMan.SprLKsMan.ALSMan; }
#endif
        public Bit81[]       Qtrue;
        public Bit81[]       Qfalse;
        public object[,]     chainDesLK;

        static  AnalyzerBaseV2( ){
            Create_ConnectedCells();
            Create_ConnectedCells128();
            __SimpleAnalyzerB__ = false;
        }
        public  AnalyzerBaseV2( ){}
        public  AnalyzerBaseV2( GNPX_AnalyzerMan pAnMan ){ 
            this.pAnMan=pAnMan;
            UCell.pAnMan = pAnMan;
            pGNPX_Eng = pAnMan.pGNPX_Eng;
        }

        public (int,int) ToTuple1( int X ) => (X>>1,X&1);

    #region Display Control
        static public readonly string[]    rcbStr=new string[]{ "r", "c", "b" };

        static public readonly Color[] _ColorsLst=new Color[]{
            Colors.LightGreen, Colors.Yellow, Colors.Aqua, 
            Colors.MediumSpringGreen, Colors.Moccasin, Colors.Pink,
            Colors.ForestGreen, Colors.Aquamarine, Colors.Beige,Colors.YellowGreen,
            Colors.Lavender, Colors.Magenta, Colors.Olive, Colors.SlateBlue, 
            Colors.LawnGreen, Colors.Orange, Colors.LimeGreen, Colors.Aquamarine };

        static public readonly Color AttCr    = Colors.Red;
        static public readonly Color AttCr2   = Colors.SkyBlue;
        static public readonly Color AttCr3   = Colors.Green;
        static public readonly Color SolBkCr  = Colors.Yellow;
        static public readonly Color SolBkCr2 = Colors.LightGreen; //Aqua;//SpringGreen//Colors.CornflowerBlue;  //FIn
        static public readonly Color SolBkCr3 = Colors.Aqua;　　   //Colors.CornflowerBlue;
        static public readonly Color SolBkCr4 = Colors.LawnGreen; //SpringGreen B81P0Hk PaleVioletRed LightSalmon Orchid PaleGreen
    #endregion Display Control      

    #region Connected Cells
        static public Bit81[] ConnectedCells;    //Connected Cells
      //static public Bit81[] ConnectedCellsRev; //Connected Cells Reverse (not use in GNPXcore!!)
        static public Bit81[] HouseCells;        //Row(0-8) Collumn(9-17) Block(18-26)
 
        static public UInt128[]  ConnectedCells128;    //Connected Cells
      //static public UInt128[]  ConnectedCellsRev128; //Connected Cells Reverse (not use in GNPXcore!!)
        static public UInt128[]  HouseCells128;        //Row(0-8) Collumn(9-17) Block(18-26)
        static public UInt128    bit81_1;
        static private void Create_ConnectedCells(){
            if(ConnectedCells!=null)  return;
            ConnectedCells    = new Bit81[81];
          //ConnectedCellsRev = new Bit81[81];

            for( int rc=0; rc<81; rc++ ){
                Bit81 BS = new Bit81();
                foreach( var q in __IEGetCellsConnectedRC(rc).Where(q=>q!=rc)) BS.BPSet(q);
                BS.BPReset(rc);

                ConnectedCells[rc] = BS;
              //ConnectedCellsRev[rc] = BS ^ 0x7FFFFFF;
            }

            HouseCells = new Bit81[27];
            for( int h=0; h<27; h++ ){
                Bit81 tmp=new Bit81();
                foreach( var q in __IEGetCellInHouse(h) ) tmp.BPSet(q);
                HouseCells[h] = tmp;
            }
        }

        static private void Create_ConnectedCells128(){
            if( ConnectedCells128 != null )  return;
            bit81_1 = ((UInt128)1<<81)-1;
            ConnectedCells128    = new UInt128[81];
          //ConnectedCellsRev128 = new UInt128[81];

            for( int rc=0; rc<81; rc++ ){
                UInt128 BS128 = 0;
                foreach( var q in __IEGetCellsConnectedRC(rc).Where(q=>q!=rc)) BS128 |= (UInt128)1<<q;
                BS128 ^= ((UInt128)1<<rc);

                ConnectedCells128[rc]    = BS128;
              //ConnectedCellsRev128[rc] = BS128 ^ bit81_1;
                        //WriteLine( $" BS128 rc:{rc} {BS128.ToString81()}");
            }

            HouseCells128 = new UInt128[27];
            for( int h=0; h<27; h++ ){
                UInt128 tmp = 0;
                foreach( var q in __IEGetCellInHouse(h) ) tmp |= (UInt128)1<<q;
                HouseCells128[h] = tmp;

                        //WriteLine( $" HouseCells128:{h} {tmp.ToString81()}");
            }
        }
        static private IEnumerable<int> __IEGetCellsConnectedRC( int rc ){ 
            int r=0, c=0;
            for(int kx=0; kx<27; kx++ ){
                switch(kx/9){
                    case 0: r=rc/9; c=kx%9; break; //row 
                    case 1: r=kx%9; c=rc%9; break; //collumn
                    case 2: int b=rc/27*3+(rc%9)/3; r=(b/3)*3+(kx%9)/3; c=(b%3)*3+kx%3; break;//block
                }
                yield return r*9+c;
            }
        }
        static private IEnumerable<int> __IEGetCellInHouse( int h ){ //nx=0...8
            int r=0, c=0, tp=h/9, fx=h%9;
            for(int nx=0; nx<9; nx++ ){
                switch(tp){
                    case 0: r=fx; c=nx; break;  //row
                    case 1: r=nx; c=fx; break;  //collumn
                    case 2: r=(fx/3)*3+nx/3; c=(fx%3)*3+nx%3; break;  //block
                }
                yield return (r*9+c);
            }
        }
/*
        static public IEnumerable<int> IEGet_rc( UInt128 bitRep ){
            int rc=0;
            UInt128 w=bitRep;
            do{
                if( (w&1) > 0 )  yield return rc; 
                w >>= 1;
            }while(w>0 && ++rc<81 );
            yield break;
        }
*/
    #endregion Connected Cells
    }
}