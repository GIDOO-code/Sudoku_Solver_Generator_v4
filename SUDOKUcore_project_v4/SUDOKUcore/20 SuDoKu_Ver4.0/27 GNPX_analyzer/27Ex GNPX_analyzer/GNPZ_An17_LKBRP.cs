using System;
using System.Collections.Generic;
//using System.Drawing;
using System.Linq;
//using System.Security.Cryptography.Xml;
using System.Windows.Media;
using static System.Diagnostics.Debug;

using GIDOO_space;

namespace GNPXcore{
    public partial class NXGCellLinkGen: AnalyzerBaseV2{

        //RemotePair is an algorithm that connects bivalue cells with a StrongLlink.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page47.html

        //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
        //.3..9.68...9.64..2..7..8.5.84.6.9....26...41....2.1.96.9.4..1..6..81.5...14.5..6.
        //2..8..1...8.4..6.3...2968...1..3.2.43.......69.5.8..3...1324...6.2..8.1...8..1..2

        public bool RemotePair( ){     //RemotePairs
			Prepare(); 
            if( BVCellLst==null )  BVCellLst = pBOARD.FindAll(p=>(p.FreeBC==2)); //BV:bivalue
            if( BVCellLst.Count<3 ) return false;  

            foreach( var (CRL,FreeB) in _RPColoring()){
                bool RPFound=false;
                foreach( var P in pBOARD.Where(p=>(p.FreeB&FreeB)>0) ){
                    if( (CRL[0]&ConnectedCells[P.rc]).IsZero() )  continue;
                    if( (CRL[1]&ConnectedCells[P.rc]).IsZero() )  continue;                  
                    P.CancelB = P.FreeB&FreeB; RPFound=true;
                }

                if( RPFound ){ //=== found ===
                    SolCode = 2;
                    string SolMsg="Remote Pair #"+FreeB.ToBitStringN(9);
                    Result=SolMsg;
                    if(!SolInfoB) return true;
                    ResultLong = SolMsg;

                    Color Cr  = _ColorsLst[0];
                    Color Cr1 = Color.FromArgb(255,Cr.R,Cr.G,Cr.B);   
                    Color Cr2 = Color.FromArgb(150,Cr.R,Cr.G,Cr.B);
                    foreach(var P in CRL[0].IEGet_rc().Select(p=>pBOARD[p]))  P.Set_CellColorBkgColor_noBit( FreeB, AttCr, Cr1 );
                    foreach(var P in CRL[1].IEGet_rc().Select(p=>pBOARD[p]))  P.Set_CellColorBkgColor_noBit( FreeB, AttCr, Cr2 );

                    if( __SimpleAnalyzerB__ )  return true;
                    if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                    RPFound = false;
                }
            }
            return false;
        }

        private IEnumerable<(Bit81[],int)> _RPColoring( ){
            if( BVCellLst.Count<4 )  yield break;
          
            // --- coloring with bivalue cells ---
            Bit81 BivalueB = new Bit81(BVCellLst); 
                       // WriteLine( $" BivalueB:{BivalueB.ToRCString()}" );      
            Bit81 usedB = new Bit81();
            var QueTupl = new Queue<(int,int)>();

            Bit81[] CRL=new Bit81[2]; 
            CRL[0]=new Bit81(); CRL[1]=new Bit81(); 
            int  rc0;
            while( (rc0=BivalueB.FindFirst_rc())>=0 ){              //Start searching from rc0
                BivalueB.BPReset(rc0);
                        // WriteLine( $" 000 BivalueB{BivalueB.ToRCString()}" );

                CRL[0].Clear(); CRL[1].Clear();                     //Clear chain
                
                QueTupl.Clear();                                    //Queue(QueTupl) initialization
                QueTupl.Enqueue( (rc0,0) );
                
                int FreeB = pBOARD[rc0].FreeB;         
                usedB.Clear();
                while( QueTupl.Count>0 ){
                    var (rc1,color1) = QueTupl.Dequeue();           //Get Current Cell
                    usedB.BPSet(rc1);
                    CRL[color1].BPSet(rc1);
                    int color2 = 1-color1;                          //color inversion

                    Bit81 Chain = BivalueB & ConnectedCells[rc1];
                    foreach( var rc2 in Chain.IEGet_rc().Where(rc=> !usedB.IsHit(rc)) ){
                        if( pBOARD[rc2].FreeB!=FreeB ) continue;
                        QueTupl.Enqueue( (rc2,color2) );
                        CRL[color2].BPSet(rc2);
                    }
                }
                
                if( CRL[1].BitCount()>0 ) yield return (CRL,FreeB);
                BivalueB -= (CRL[0]|CRL[1]);
                         // WriteLine( $"{(CRL[0]|CRL[1]).ToRCString()}" );
                         // WriteLine( $" 111 BivalueB{BivalueB.ToRCString()}" );
            }
            yield break;
        }
    }
}