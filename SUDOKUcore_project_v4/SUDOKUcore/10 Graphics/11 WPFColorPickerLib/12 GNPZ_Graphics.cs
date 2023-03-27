using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;

using static System.Math;
using static System.Diagnostics.Debug;

using System.Windows;
using System.Windows.Controls;

using System.Windows.Media;
using System.Windows.Media.Imaging;

using System.Windows.Shapes;

using GIDOO_space;
using System.Threading;

//Reverse sample code
//http://msdn.microsoft.com/ja-jp/ff363212#05

//[C#/XAML] Draw graphics with WPF
//http://code.msdn.microsoft.com/windowsdesktop/CVBXAML-WPF-Windows-WPF-0738a600

namespace GNPXcore{
    public class GFont{
        public FontFamily FontFamily;
        public int        FontSize;
        public FontWeight FontWeight;
        public FontStyle  FontStyle;

        public string Name{ get=> FontFamily.ToString(); }

        public GFont( string FontName, int FontSize=10 ){
            this.FontFamily =new FontFamily(FontName);
            this.FontSize   = FontSize;
            this.FontWeight = FontWeights.Normal;
            this.FontStyle  = FontStyles.Normal;
        }   

        public GFont( string  FontName, int FontSize, FontWeight FontWeight, FontStyle FontStyle){
            this.FontFamily = new FontFamily(FontName);
            this.FontSize   = FontSize;
            this.FontWeight = FontWeight;
            this.FontStyle  = FontStyle;
        }
    }
    
    public class EColor{
        static private readonly Color _Black_ = Colors.Black; 
        public Color ClrCellBkg=_Black_;
        public int   noB;
        public Color ClrDigit=_Black_;
        public Color ClrDigitBkg=_Black_;


        public EColor( Color ClrCellBkg, int noB, Color ClrDigit, Color ClrDigitBkg ){
            this.ClrCellBkg=ClrCellBkg;
            this.noB=noB; this.ClrDigit=ClrDigit; this.ClrDigitBkg=ClrDigitBkg;
        }

        public EColor( EColor E ){ ClrCellBkg=E.ClrCellBkg; noB=E.noB; ClrDigit=E.ClrDigit; ClrDigitBkg=E.ClrDigitBkg; }

        static public bool operator==( EColor A, EColor B ){
            try{
                if( B is null )  return false;
                bool eq = A.noB==B.noB && A.ClrDigit==B.ClrDigit && A.ClrDigitBkg==B.ClrDigitBkg && A.ClrCellBkg==B.ClrCellBkg;
                return eq;
            }
            catch(NullReferenceException ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); return true; }
        }
        static public bool operator!=( EColor A, EColor B ){
            try{
                if( B is null )  return true;
                bool eq = A.noB!=B.noB || A.ClrDigit!=B.ClrDigit || A.ClrDigitBkg!=B.ClrDigitBkg || A.ClrCellBkg!=B.ClrCellBkg;
                return eq;
            }
            catch(NullReferenceException ex){ WriteLine(ex.Message+"\r"+ex.StackTrace); return true; }
        }
        public override string ToString(){
            string st = $" no:{noB.ToBSt()} CellBkg:{ClrCellBkg} DigClr:{ClrDigit} DigBkgClr:{ClrDigitBkg}";
            return st;
        }

    }
    
    public class GFormattedText{
        private CultureInfo CulInfoJpn=CultureInfo.GetCultureInfo("ja-JP");
        private Typeface TFace; //##
        private GFont    _GFont;
        public GFont gFnt{
            set{
                _GFont = value; 
                TFace  = new Typeface( _GFont.FontFamily, _GFont.FontStyle, _GFont.FontWeight, FontStretches.Medium );
            }
        }

        public GFormattedText( GFont gf ){
            gFnt = gf;
        }

        public FormattedText GFText( string st, Brush br ){
            var pixelsPerDip = GNPX_App.pixelsPerDip;
            FormattedText FT = new FormattedText(st,CulInfoJpn,FlowDirection.LeftToRight,TFace,_GFont.FontSize,br,pixelsPerDip );                             
            return FT;
        }
    }
    
    public class GNPZ_Graphics{
        static private readonly Color _Black_ = Colors.Black; 
        static private GFont gFnt12 = new GFont("ＭＳ　ゴシック",12,FontWeights.Medium,FontStyles.Normal);
        static private GFormattedText GF8 = new GFormattedText( gFnt12 );
        static private CultureInfo CulInfoJpn=CultureInfo.GetCultureInfo("ja-JP"); 
        static public  bool overlaped;

        private NuPz_Win   pGNP00win;
        private GNPX_App pGNP00;
        private Dictionary<string,Color> pColorDic;      

        public GNPZ_Graphics( GNPX_App pGNP00 ){
            this.pGNP00 = pGNP00;
            this.pGNP00win = pGNP00.pGNP00win;
            this.pColorDic = GNPX_App.ColorDic;
        }

        public void GBoardPaint( RenderTargetBitmap bmp, List<UCell> qBDL, string GSmodeX="", bool sNoAssist=false, bool whiteBack=false ){
            if(qBDL==null || bmp==null)  return;

            int   LWid  = pGNP00.lineWidth;
            int   CSiz  = pGNP00.cellSize;
            int   CSizP = CSiz+LWid;

            Brush brBL = new SolidColorBrush(pColorDic["BoardLine"]);
            Brush brBoad = new SolidColorBrush(pColorDic["Board"]);
            Brush brFNo  = new SolidColorBrush(pColorDic["CellForeNo"]);
            Brush brPNo  = new SolidColorBrush(pColorDic["CellBkgdPNo"]);    
            Brush brMNo  = new SolidColorBrush(pColorDic["CellBkgdMNo"]);    
            Brush brZNo  = new SolidColorBrush(pColorDic["CellBkgdZNo"]); 
            if(whiteBack){
                brBL=brFNo = Brushes.Black; 
                brBoad=brPNo=brMNo=brZNo = Brushes.White;
            }

            //MS Gothic
            GFont gFnt32 = new GFont( "Courier", 32, FontWeights.DemiBold, FontStyles.Normal );
            GFormattedText GF32 = new GFormattedText( gFnt32 );           
            Point pt = new Point();

            overlaped = false; 
            var drawVisual = new DrawingVisual();
            using( DrawingContext DC=drawVisual.RenderOpen() ){
                
                //Initialize
                DC.DrawRectangle(brBoad,null,new Rect(0,0,bmp.Width,bmp.Height));

                if( qBDL.Any(p=>p.No!=0) ){ 
                  #region Draw digit on board
                    // ProblemCell(No>0) SolvedCell(No<0)
                    Rect Rrct = new Rect(0,0, CSiz,CSiz);
                    foreach( UCell P in qBDL.Where(p=>p.No!=0) ){
                        int r=P.r, c=P.c;
                        pt.X = LWid + c*CSizP + (c/3);
                        pt.Y = LWid + r*CSizP + (r/3);
                                              
                        Brush br = (P.No>0)? brPNo: brMNo;
                        Rrct.X=pt.X+1; Rrct.Y=pt.Y+1;
                        DC.DrawRectangle(br,null,Rrct);
                  
                        string NoStr = Abs(P.No).ToString();
                        pt.X+=10; pt.Y+=2;
                        DC.DrawText(GF32.GFText(NoStr,brFNo),pt);
                    }
                       
                    //Unsolved
                    if(sNoAssist){
                        foreach( UCell P in qBDL.Where(p=>p.No==0) ){
                            int r=P.r, c=P.c;
                            pt.X = LWid + c*CSizP + (c/3);
                            pt.Y = LWid + r*CSizP + (r/3);
                            Rrct.X=pt.X+1; Rrct.Y=pt.Y+1;

                            RenderTargetBitmap　Rbmp = CreateCellImage(P,true);
                            DC.DrawImage(Rbmp,Rrct);
                        }
                    }
                  #endregion
                }

              #region Draw line on board

                Pen   pen, pen1=new Pen(brBL,1), pen2=new Pen(brBL,2);               
                Point ptS, ptE;
                int hh=1;
                for(int k=0; k<10; k++ ){
                    ptS=new Point(0,hh); ptE=new Point(CSiz*10-2,hh);
                    pen = ((k%3)==0)? pen2: pen1;
                    DC.DrawLine(pen,ptS,ptE);
                    hh += CSizP + (k%3)/2;
                }

                hh=1;
                for(int k=0; k<10; k++ ){
                    ptS=new Point(hh,0); ptE=new Point(hh,CSiz*10-2);
                    pen = ((k%3)==0)? pen2: pen1;
                    DC.DrawLine(pen,ptS,ptE);
                    hh += CSizP + (k%3)/2;
                }
              #endregion

            }    
            bmp.Clear();        
            bmp.Render(drawVisual);
            return;
        }

        public RenderTargetBitmap CreateCellImage( UCell P, bool candDisp ){
            Color clrFore   = pColorDic["CellForeNo"];
            Color clrFixNo  = pColorDic["CellFixedNo"];
            Color clrBkgFix = pColorDic["CellBkgdFix"];
            Brush br;
            var bmp = new RenderTargetBitmap(35,35, 96,96, PixelFormats.Default);

        //    if(P.ECrLst==null)  P.ECrLst=new List<EColor>(); 

            EColor EC;
            var drawVisual=new DrawingVisual();
            using( var DC=drawVisual.RenderOpen() ){

                // ----- backgroud Color -----
                Color ClrCellBkg=_Black_;
                if( !(P.ECrLst is null) ){
                    EC = P.ECrLst.FindLast( p => (p.ClrCellBkg!=_Black_) );     
                    if( !(EC is null) )  ClrCellBkg = EC.ClrCellBkg;    //(Last setted background color)
                }

                // ----- fixed/canceled Digit ---- 
                if( P.FixedNo>0 ){ //When the digit is true, the "confirmed color" is used.)
                    ClrCellBkg = clrBkgFix;
                    P.ECrLst_Add( new EColor( clrBkgFix, (1<<(P.FixedNo-1)), clrFixNo, _Black_ ) );
                }
                else if( P.CancelB>0 ){ //(canceled)
                    //If the digit is false, display in white. No background color is specified.)
                    P.ECrLst_Add( new EColor( _Black_,  P.CancelB, _Black_, clrFixNo ) ); //reverse
                }

                // ----- draw cell background
                // If the background color is specified, draw the background color of the cell.
                if( ClrCellBkg != _Black_ ){
                    br = new SolidColorBrush(ClrCellBkg);
                    DC.DrawRectangle( br, null, new Rect( 0,0, bmp.Width, bmp.Height) );
                }

                // ----- small digits (candidate digits) -----
                int dspB=0; 
                List<EColor> ECrLst=P.ECrLst;
                if( !(ECrLst is null) ){
                    foreach( int no in P.FreeB.IEGet_BtoNo() ){
                        int   noB=(1<<no), noP=no+1, x=(no%3)*12, y=(no/3)*12;
                        Point pt = new Point(x+3,y);                   

                        Color ClrDigit=clrFore;
                        EC = ECrLst.FindLast(p=> (p.ClrDigitBkg!=_Black_) && (p.noB&noB)>0 ); // last setting of digit(no) color. 
                        if( !(EC is null) ){ // --- draw the background color of the digit   
                            Brush brBg = new SolidColorBrush(EC.ClrDigitBkg); // digit background color
                            Rect  re =new Rect(x+1,y,12,12); 
                            DC.DrawRectangle( brBg, null, re ); // background colored squares
                            ClrDigit = Colors.White;            // digit drawing color
                        }
                        else{   
                            EC = ECrLst.FindLast(p=> (p.ClrDigit!=_Black_) && (p.noB&noB)>0 );
                            if( !(EC is null) ) ClrDigit = EC.ClrDigit; // digit drawing color
                        }

                        if( ClrDigit != _Black_ ){  
                            br = new SolidColorBrush(ClrDigit); // digit drawing color Brush
                            DC.DrawText(GF8.GFText( noP.ToString(), br ), pt);
                            dspB |= (1<<no); //(treated)
                        }
                    }
                }

                // ----- uncolored digits
                if( (dspB=(P.FreeB).DifSet(dspB)) >0 ){
                    br = new SolidColorBrush(clrFore);
                    foreach(int no in dspB.IEGet_BtoNo()){
                        int   noB=(1<<no), x=(no%3)*12, y=(no/3)*12;
                        Point pt = new Point(x+3,y);
                        int noP=no+1;
                        DC.DrawText( GF8.GFText(noP.ToString(), br), pt );
                    }
                }

                // ----- overlap ----
                if( P.ECrLst!=null ){
                    var X = P.ECrLst.GroupBy(p=>p.ClrCellBkg).ToList();
                    if( X!=null && X.Count(q=> ((Color)q.Key)!=_Black_)>=2 ){
                        Pen   pn  = new Pen( Brushes.DarkOrange,3); 
                        Point pt  = new Point(3,3);
                        DC.DrawEllipse( null, pn, pt, 2,2 );
                        GNPZ_Graphics.overlaped = true;
                    }
                }
            }
            bmp.Render(drawVisual);

            return bmp;
        }
        public RenderTargetBitmap CreateCellImageLight( UCell P, int noX ){
            Color clrFix = pColorDic["CellFixed"];
            Color crFree = pColorDic["CellForeNo"];
            Brush br     = new SolidColorBrush(pColorDic["CellBkgdZNo2"]);
            var bmp = new RenderTargetBitmap(35,35, 96,96, PixelFormats.Default);

            var drawVisual=new DrawingVisual();
            using(var DC=drawVisual.RenderOpen()){
                DC.DrawRectangle( br, null, new Rect( 0,0, bmp.Width, bmp.Height) );
                foreach( int no in P.FreeB.IEGet_BtoNo() ){
                    int noP=no+1, x=(no%3)*12, y=(no/3)*12;
                    Color cr=(noP==noX)? clrFix: crFree; 
                    Point pt=new Point(x+3,y);
                    br = new SolidColorBrush(cr);
                    DC.DrawText(GF8.GFText( noP.ToString(), br ), pt);
                }
            }          
            bmp.Render(drawVisual);
            return bmp;
        }

        public void GBPatternPaint( Canvas can, /*colorListWPF CRL,*/ int[,] GPat ){
            int   csz =pGNP00.cellSize/2;
            int   cszP=csz+1;
            Point ptS, ptE;
            Brush brBoad = new SolidColorBrush(pColorDic["Board"]);
            Brush br = new SolidColorBrush(pColorDic["CellBkgdPNo"]);

            can.Children.Clear();
            Rectangle rct=new Rectangle();
            rct.Fill=brBoad; rct.Height=can.Width; rct.Width=can.Height;
            can.Children.Add(rct);

            for(int r=0; r<9; r++ ){
                for(int c=0; c<9; c++ ){
                    if(GPat[r,c]==0) continue;
                    Rectangle rcPatt = new Rectangle();
                    rcPatt.Fill=br;
                    ptS= new Point(c*cszP+c/3+2,r*cszP+r/3+2);
                    rcPatt.Margin=new Thickness(ptS.X, ptS.Y,0.0,0.0);
                    rcPatt.Height=rcPatt.Width=csz;
                    can.Children.Add(rcPatt);
                }
            }

            //===== Draw line on board =====
            Brush brBL = new SolidColorBrush(pColorDic["BoardLine"]);
            int wd, hh;

            hh=1;
            for(int k=0; k<10; k++ ){
                ptS=new Point(0,hh); ptE=new Point(csz*10-4,hh);
                wd = ((k%3)==0)? 2: 1;
                Line Ln=LinePlotter(ptS,ptE,brBL,wd,0);
                can.Children.Add(Ln);
                hh += cszP+(k%3)/2;
            }

            hh=1;
            for(int k=0; k<10; k++ ){
                ptS=new Point(hh,0); ptE=new Point(hh,csz*10-4);
                wd = ((k%3)==0)? 2: 1;
                Line Ln=LinePlotter(ptS,ptE,brBL,wd,0);
                can.Children.Add(Ln);
                hh += cszP+(k%3)/2;
            }
            return;
        }
        
        public void GBPatternDigit(RenderTargetBitmap bmp, int[] SDK81){
            int[,] GPat = new int[9,9];
            for(int rc=0; rc<81; rc++ ){
                int n=SDK81[rc];
                GPat[rc/9,rc%9]=(n>9)? 0: n;
            }
            GBPatternDigit(bmp,GPat);
        }

        public void GBPatternDigit(RenderTargetBitmap bmp, int[,] GPat){
            int   LWid=pGNP00.lineWidth/2;
            int   csz =pGNP00.cellSize/2;
            int   cszP=csz+1;

            bmp.Clear();
            GFont gFnt16 = new GFont( "Courier", 16, FontWeights.Normal, FontStyles.Normal );
            GFormattedText GF16 = new GFormattedText( gFnt16 );

            var drawVisual = new DrawingVisual();
            using( DrawingContext DC=drawVisual.RenderOpen() ){           
                Brush br=new SolidColorBrush(Colors.DarkBlue);
                Point pt=new Point();
                for(int rc=0; rc<81; rc++ ){
                    int r=rc/9, c=rc%9;
                    int No=GPat[r,c];
                    if(No==0)  continue;
                    pt.X=c*19+c/3+6;
                    pt.Y=r*19+r/3+2;
                    DC.DrawText( GF16.GFText(No.ToString(),br), pt );
                }
            } 
            bmp.Render(drawVisual);
        }

        public Line LinePlotter( Point ptS, Point ptE, Brush br, int wd/*width*/, int sCode/*line type*/ ){
            try{  
                Line ln = new Line();
                ln.Stroke=br; ln.StrokeThickness = wd;
                ln.X1=ptS.X; ln.Y1=ptS.Y; ln.X2=ptE.X; ln.Y2=ptE.Y;
                

			    switch(sCode){
				    case 0: break; //line
				    case 1: ln.StrokeDashArray = new DoubleCollection{4,4}; break;	        //dotted line
				    case 2: ln.StrokeDashArray = new DoubleCollection{4,2,2,2}; break;	    //dash-dotted line
				    case 3: ln.StrokeDashArray = new DoubleCollection{4,2,2,2,2,2}; break;	//Two-dot chain line
                    case 4: ln.StrokeDashArray = new DoubleCollection{4,2}; break;	        //dotted line
			    }
                return ln;
            }
            catch(Exception ex){ WriteLine( ex.Message+"\r"+ex.StackTrace ); }
            return null;
		}
    
        public void GBoardPaintPrint( RenderTargetBitmap bmp, UPuzzle aPZL ){
            if( aPZL==null )  return;

            int   LWid =pGNP00.lineWidth;
            int   CSiz =pGNP00.cellSize;
            int   CSizP=CSiz+LWid;
            
            Brush brBoad=new SolidColorBrush(Colors.White);
            Point ptS, ptE;

            //RenderTargetBitmap Rbmp;
            Rect Rrct = new Rect(0,0, CSiz,CSiz);
          
            //Courier
            GFont gFnt32 = new GFont("Courier", 32, FontWeights.DemiBold, FontStyles.Normal);
            GFormattedText GF32 = new GFormattedText( gFnt32 );

            var drawVisual = new DrawingVisual();
            using(DrawingContext DC=drawVisual.RenderOpen()){           
                DC.DrawRectangle(brBoad, null, new Rect(0,0, bmp.Width,bmp.Height));

                Brush br=new SolidColorBrush(_Black_);
                Point pt=new Point();

                foreach( UCell BDX in aPZL.BOARD ){
                    int r=BDX.r, c=BDX.c;
                     
                    pt.X = c*CSizP + LWid/2 + (c/3);
                    pt.Y = r*CSizP + LWid/2 + (r/3);
                    Rrct.X=pt.X;　Rrct.Y=pt.Y;

                    int No=BDX.No;
                    if(No!=0){
                      #region Problem/solved cell
                        pt.X += 10; pt.Y += 2;
                        string NoStr = Abs(No).ToString();
                        DC.DrawText(GF32.GFText(NoStr,br), pt);
                      #endregion
                    }
                }
                #region Draw line on board
                Pen pen1=new Pen(br,1), pen2=new Pen(br,2), pen;

                int hh=1;
                for(int k=0; k<10; k++ ){
                    ptS=new Point(0,hh);
                    ptE=new Point(CSiz*10-2,hh);
                    pen=((k%3)==0)? pen2: pen1;
                    DC.DrawLine(pen,ptS,ptE);
                    hh += CSizP+(k%3)/2;
                }

                hh=1;
                for(int k=0; k<10; k++ ){
                    ptS=new Point(hh,0);
                    ptE=new Point(hh,CSiz*10-2);
                    pen=((k%3)==0)? pen2: pen1;
                    DC.DrawLine(pen,ptS,ptE);
                    hh += CSizP+(k%3)/2;
                }
                #endregion
            }                            
            bmp.Render(drawVisual);

            return;
        }
    }
}
