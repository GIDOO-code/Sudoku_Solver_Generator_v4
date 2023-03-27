using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using System.Windows.Shapes;
using static System.Diagnostics.Debug;

using GIDOO_space;
using System.Security.Cryptography;
using System.Runtime.CompilerServices;

namespace GNPXcore{
    public partial class NXGCellLinkGen: AnalyzerBaseV2{

        //Coloring is an algorithm that connects the focused digit with a strong link.
        // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page46.html

        public bool Color_Trap( ){
            //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
            //4....9.5.23..58.67...4.7.........3253.2....8.5.1...7.....89....9......7..1.72..46  by /Solve/MultiSolve step19
            //4762391582391584671..4672397.....3253.2.7.6815613827946.789.51.9.....87.81.72.946  for develop.

			Prepare();
			CeLKMan.PrepareCellLink(1);                                 //Generate StrongLink

            Bit81 HitB = new Bit81();

            int testCC=0;   // #####
            for(int no=0; no<9; no++ ){
                int noB=(1<<no);
                foreach( Bit81[] CRL in _Coloring(no) ){                //_Coloring: cells_network connected by strong-links

                    HitB.Clear();
                    Bit81 ELM = (new Bit81(pBOARD,noB))-(CRL[0]|CRL[1]);  //ELM:Exclude cells from CRL_cells with digit(no)
                    foreach( var rc in ELM.IEGet_rc() ){                //rc:Candidate cell  
                        Bit81 CNb = ConnectedCells[rc];                     //Cells not included in Network
                        if( (CNb&CRL[0]).IsZero() ) continue;               //Exclude Cells related to Network
                        if( (CNb&CRL[1]).IsZero() ) continue;
                        HitB.BPSet(rc);                                 //solution cell
                    }

                    if( HitB.IsNotZero() ){ //===== found =====
                        foreach( var P in HitB.IEGet_rc().Select(p=>pBOARD[p]) )   P.CancelB=noB; 
                        
                        SolCode = 2;
                        string SolMsg="Coloring Trap #"+(no+1);
                        Result = SolMsg;
                        if( SolInfoB ){
                            string st1 = $"\r group 1 : {CRL[0].ToRCString_withComp()}";
                            st1 += $"\r group 2 : {CRL[1].ToRCString_withComp()}";
                            st1 += $"\r exclude : {HitB.ToRCString_withComp()}#{no+1}";

                            ResultLong = SolMsg + st1;
                        
                            Color Cr =_ColorsLst[1];
                            Color Cr1=Cr; Cr1.A=(byte)127;                  //Color.FromArgb(120,Cr.R,Cr.G,Cr.B);  
                            CRL[0].IE_SetNoBBgColor( pBOARD, noB, AttCr, Cr );
                            CRL[1].IE_SetNoBBgColor( pBOARD, noB, AttCr, Cr1 );
                        }
                        if( __SimpleAnalyzerB__ )  return true;
                        if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                        HitB=new Bit81();
                    }
                }
            }

            return false;
        }

        public bool Color_Wrap( ){
            //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/    
            //..9..154..5..9.....6.5..92.8..3..1.41..4.6..95.4..8..2.42..9.6.....6..1..187..2..  by /Solve/MultiSolve step3

			Prepare();
			CeLKMan.PrepareCellLink(1);                                 //Generate StrongLink

            for(int no=0; no<9; no++ ){
                int noB=(1<<no);
                Bit81 BD0 = new Bit81(pBOARD,noB);

                foreach( Bit81[] CRL in _Coloring(no) ){                // _Coloring: cells_network connected by strong-links
                    if( CRL[0].Count<=1 || CRL[1].Count<=1)  continue;

                    Bit81 ELM = BD0 - (CRL[0]|CRL[1]);                    // ELM:Elements not included in the network
                    if(ELM.Count==0) continue;

                    for(int k=0; k<2; k++ ){
                        
                        for(int h=0; h<27; h++ ){
                            if( (CRL[k]&HouseCells[h]).Count<2 ) continue;
                            //There are two Cs of the same color. If this is true it will break the network.

                            //===== found =====
                            foreach( var Q in CRL[k].IEGet_rc().Select(p=>pBOARD[p]) ) Q.CancelB=noB; 
                            
                            SolCode = 2;
                            string SolMsg="Coloring Wrap #"+(no+1);
                            Result=SolMsg;
                            if( SolInfoB ){
                                string st1 = $"\r group 1 : {CRL[1-k].ToRCString_withComp()}";
                                st1 += $"\r group 2 : {CRL[k].ToRCString_withComp()}";
                                st1 += $"\r exclude : {CRL[k].ToRCString_withComp( $"#{no+1}" )}";

                                ResultLong = SolMsg + st1;
                                Color Cr  = _ColorsLst[1];
                                Color Cr1 = Cr;     
                                Color Cr2 = Color.FromArgb(120,Cr.R,Cr.G,Cr.B);
                                CRL[1-k].IE_SetNoBBgColor( pBOARD, noB, AttCr, Cr1 );
                                CRL[k].IE_SetNoBBgColor(   pBOARD, noB, AttCr, Cr2 );
                            }
                            if( __SimpleAnalyzerB__ )  return true;
                            if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                        }
                    }
                }
            }
            return false;
        }

        //=================== MultiColoring ==================================
        public bool MultiColor_Type1( ){
            //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
            //..9..154..5..9.....6.5..92.8..3..1.41..4.6..95.4..8..2.42..9.6.....6..1..187..2..  by /Solve/MultiSolve step8

			Prepare();
			CeLKMan.PrepareCellLink(1);    //Generate StrongLink
            
            for(int no=0; no<9; no++ ){
                int noB=(1<<no);
                Bit81 BD0=new Bit81(pBOARD,noB);
                List<Bit81[]> MCRL = _Coloring(no:no, minSize:2, bMulti:true).ToList();

                if( MCRL==null || MCRL.Count<2 ) continue;

                var cmb=new Combination(MCRL.Count,2);
                while( cmb.Successor() ){
                    Bit81[] CRLa = MCRL[cmb.Index[0]];      // group_A
                    Bit81[] CRLb = MCRL[cmb.Index[1]];      // group_B

                    for(int na=0; na<2; na++ ){
                        Bit81 HCRLa = new Bit81();
                        foreach(var rc in CRLa[na].IEGet_rc()) HCRLa |= ConnectedCells[rc]; // HCRLa: cell group connected to group_A[na]

                        for(int nb=0; nb<2; nb++ ){
                            if( (HCRLa&CRLb[nb]).IsZero() ) continue;
                            // HCRLa and group_B[nb] are weakly connected

                            Bit81 ELMtry = BD0 - (CRLa[na] | CRLb[nb] | CRLa[1-na] | CRLb[1-nb]); // Cells unrelated to group_A and group_A
                            if(ELMtry.Count==0) continue;
                            
                            bool found=false;
                            Bit81 ELM=new Bit81();
                            foreach( var rc in ELMtry.IEGet_rc() ){
                                if( !ConnectedCells[rc].IsHit(CRLa[1-na]) ) continue;
                                if( !ConnectedCells[rc].IsHit(CRLb[1-nb]) ) continue;
                                pBOARD[rc].CancelB=noB; ELM.BPSet(rc); found=true;
                            }
                            if(found){
                                SolCode = 2;
                                string SolMsg="MultiColoring Type1 #"+(no+1);
                                Result=SolMsg;
                                if( SolInfoB ){
                                    Color CrA = _ColorsLst[4];
                                    foreach( var P in ELM.IEGet_rc().Select(p=>pBOARD[p]) ) P.Set_CellColorBkgColor_noBit(noB,AttCr,CrA);
                                    string st1 = "";
                                    for(int k=0; k<2; k++){
                                        Bit81[] CRLX = MCRL[cmb.Index[k]];
                                        Color Cr1=_ColorsLst[k*2];
                                        Color Cr2=Cr1; Cr2.A=(byte)100;          
                                        CRLX[1-k].IE_SetNoBBgColor( pBOARD, noB, AttCr, Cr1 );
                                        CRLX[k].IE_SetNoBBgColor(   pBOARD, noB, AttCr, Cr2 );

                                        st1 +=  $"\r group {k+1}-A: {CRLX[0].ToRCString_withComp( $"#{no+1}" )}";
                                        st1 +=  $"\r group {k+1}-B: {CRLX[1].ToRCString_withComp( $"#{no+1}" )}";
                                    }
                                    st1 +=  $"\r exclude : {ELM.ToRCString_withComp( $"#{no+1}" )}";

                                    ResultLong = SolMsg + st1;
                                }
                                if( __SimpleAnalyzerB__ )  return true;
                                if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                            }
                        }
                    }
                }
            }
            return false;    
        }

        public bool MultiColor_Type2( ){
            //Paste the next 81 digits onto the grid and solve with /Solve/MultiSolve/
            //...6.8...6...9...529.....483.1...4.64..3.1..2...8.6....1.4.2.7..6.7.9.5.....8....  by /Solve/MultiSolve step11

			Prepare();
			CeLKMan.PrepareCellLink(1);    //Generate StrongLink
            for(int no=0; no<9; no++ ){
                int noB=(1<<no);
                List<Bit81[]> MCRL = _Coloring(no:no,minSize:1,bMulti:true).ToList();
                if(MCRL==null || MCRL.Count<2)  continue;

                var prm=new Permutation(MCRL.Count,2);
                while(prm.Successor()){
                    Bit81[] CRLa = MCRL[prm.Index[0]];
                    Bit81[] CRLb = MCRL[prm.Index[1]];

                    //*** Choose network "a"
                    Bit81 HCRL0=new Bit81(), HCRL1=new Bit81();
                    foreach( var rc in CRLa[0].IEGet_rc() ) HCRL0 |= ConnectedCells[rc];  //all elements connected to a[0] -> HCRL0
                    foreach( var rc in CRLa[1].IEGet_rc() ) HCRL1 |= ConnectedCells[rc];  //all elements connected to a[1] -> HCRL1

                    for(int nb=0; nb<2; nb++ ){
                        //*** Choose network "b"
                        if( (CRLb[nb]&HCRL0).IsZero() ) continue;                           //b[nb] is not connected to HCRL0
                        if( (CRLb[nb]&HCRL1).IsZero() ) continue;                           //b[nb] is not connected to HCRL1
                        //Both CRLa[0] and CRLa[1] have cells in CRLb[nb] connected by a weak link.

                        //===== found =====
                        SolCode = 2;
                        foreach( var P in CRLb[nb].IEGet_rc().Select(p=>pBOARD[p]) ) P.CancelB=noB;                       

                        string SolMsg="MultiColoring Type2 #"+(no+1);
                        Result=SolMsg;
                        if( SolInfoB ){

                            Color Cr1 = _ColorsLst[0];   
                            Color Cr2 = Color.FromArgb( 100, Cr1.R, Cr1.G, Cr1.B );
                            CRLa[0].IE_SetNoBBgColor( pBOARD, noB, AttCr, Cr1 );
                            CRLa[1].IE_SetNoBBgColor( pBOARD, noB, AttCr, Cr2 );

                            Cr1 = _ColorsLst[1];   
                            Cr2 = Color.FromArgb( 100, Cr1.R, Cr1.G, Cr1.B );
                            CRLb[1-nb].IE_SetNoBBgColor( pBOARD, noB, AttCr, Cr2 );
                            CRLb[nb].IE_SetNoBBgColor(   pBOARD, noB, AttCr, Cr1 );

                            string st1 = $"\r group 1-A: {CRLa[0].ToRCString_withComp( $"#{no+1}" )}";
                            st1 += $"\r group 1-B: {CRLa[1].ToRCString_withComp( $"#{no+1}" )}";
    
                            st1 +=  $"\r group 2-A: {CRLb[1-nb].ToRCString_withComp( $"#{no+1}" )}";
                            st1 += $"\r group 2-B: {CRLb[nb].ToRCString_withComp( $"#{no+1}" )}";   
        
                            st1 +=  $"\r exclude : {CRLb[nb].ToRCString_withComp( $"#{no+1}" )}";
                            
                            ResultLong = SolMsg + st1;
                        }
                        
                        if( __SimpleAnalyzerB__ )  return true;
                        if( !pAnMan.SnapSaveGP(pPZL) )  return true;
                    }
                }
            }
            return false;    
        }    

#region Abstraction code version
        //Generate a cells_network connected by strong-links
        private IEnumerable<Bit81[]> _Coloring( int no, int minSize=1, bool bMulti=false ){ 
            ColoringQueue QX = new ColoringQueue(pBOARD,no,bMulti);
            int _rc1, _color, S=1;                      //S=1:strong link
            while( (_rc1=QX.FindFirst_rc()) >= 0 ){     // _rc1:Starting point cell for coloring
                                                        
                while( QX.Count>0 ){
                    (_rc1,_color) =  QX.Dequeue();      // dequeue the next node and color

                    int _color2 = 1-_color;             // invert color
                    foreach( var P in CeLKMan.IEGetRcNoType(_rc1,no,S) ){  //find link connecting to _rc1.
                        QX.CheckAndEnqueue( P, _color2 );       // If P is not connected, expand the network.
                    }
                }
                if( QX.CRL[0].Count>=minSize && QX.CRL[1].Count>=minSize )  yield return QX.CRL;
                QX.PreparingForNextStep();
            }
            yield break;
        }

        private class ColoringQueue{
            private Queue<int>  Que;
            private Bit81       BPno;                   // Bit representation of the cells with #no as a candidate
            public  Bit81[]     CRL;                    // colored network
            private bool        bMulti;
            public int          Count => Que.Count;

            public ColoringQueue( List<UCell> pBOARD, int no, bool bMulti=false  ){
                this.Que  = new Queue<int>();
                this.BPno = new Bit81(pBOARD,(1<<no));
                this.bMulti = bMulti;
                CRL = new Bit81[2]; CRL[0]=new Bit81(); CRL[1]=new Bit81();
            }

            public void PreparingForNextStep(){
                BPno -= ( CRL[0] | CRL[1] );                // Remove evaluated Cells
                if( bMulti ){
                    CRL = new Bit81[2]; CRL[0]=new Bit81(); CRL[1]=new Bit81();
                }
            }

            public int FindFirst_rc(){
                int _rc1 = BPno.FindFirst_rc();

                if( _rc1>=0 ){     
                    Que.Clear();
                    Enqueue( _rc1, 0 );                 // starting point cell for coloring   

                    // initialize network
                    CRL[0].Clear(); CRL[1].Clear();
                    CRL[0].BPSet(_rc1);                 // record first node(cell) in network
                }
                return _rc1;
            }

            public (int,int) Dequeue( ){ 
                int _X = Que.Dequeue();                 // === deque ===
                return ( _X>>1, _X&1 );                 // ( cell, color)
            }

            public void CheckAndEnqueue( UCellLink P, int _color2 ){ 
                int _rc2 = P.rc2;

                if( !(CRL[0]|CRL[1]).IsHit(_rc2) ){    // If P is not connected,           
                    CRL[_color2].BPSet(_rc2);          //   expand the network.   
                    Enqueue( _rc2, _color2);
                }
            }

            private void Enqueue( int _rc2, int _color2){
                Que.Enqueue( _rc2<<1 | _color2 );
            }
        }
#endregion Abstraction code version

#region Naive code version
        private IEnumerable<Bit81[]> _Coloring_Basic( int no ){
            Bit81[] CRL=new Bit81[2];               
            CRL[0]=new Bit81(); CRL[1]=new Bit81(); 
            Bit81 BPno = new Bit81(pBOARD,(1<<no));
            int  rc1;
            while( (rc1=BPno.FindFirst_rc()) >= 0 ){

                Queue<int> rcQue=new Queue<int>();
                rcQue.Enqueue(rc1<<1);

                CRL[0].Clear(); CRL[1].Clear();
                CRL[0].BPSet(rc1);
                while(rcQue.Count>0){
                    rc1 = rcQue.Dequeue();
                    int kx=1-(rc1&1);        //
                    rc1 >>= 1;
                    foreach( var P in CeLKMan.IEGetRcNoType(rc1,no,1) ){
                        int rc2=P.rc2;
                        if(!(CRL[0]|CRL[1]).IsHit(rc2) ){
                            CRL[kx].BPSet(rc2); 
                            rcQue.Enqueue((rc2<<1)|kx);
                        }
                    }
                }
                yield return CRL;
                BPno -= ( CRL[0] | CRL[1] );
            }
            yield break;
        }
#endregion Naive code version

    }
}
