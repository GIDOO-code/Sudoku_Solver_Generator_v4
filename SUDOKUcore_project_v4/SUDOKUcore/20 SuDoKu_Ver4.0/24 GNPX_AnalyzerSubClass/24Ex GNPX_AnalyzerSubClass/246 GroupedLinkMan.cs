using System;
using System.Collections.Generic;
using System.Linq;
using static System.Diagnostics.Debug;

namespace GNPXcore {
    public class GroupedLinkMan{

        // GroupedLinkMan
        //  In Sudoku's various solutions, Strong and Weak links between cells are used.
        //  There are various kinds of links besides inter-cell links.
        //  Grouped Cells link is a link by arrangement of multiple cells of block.
        //  https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page28.html

        static public Bit81[] pHouseCells{ get=> AnalyzerBaseV2.HouseCells; }
        private const int     S=1, W=2;
        private GNPX_AnalyzerMan pAnMan;
        private List<UCell>   pBOARD{ get=> pAnMan.pBOARD; }
        public  int           SWCtrl;
        public List<GroupedLink> GrpCeLKLst;
        private int[]         bitRC=new int[9];

        public GroupedLinkMan( GNPX_AnalyzerMan pAnMan ){
            this.pAnMan  = pAnMan;               
            SWCtrl=0;

            for(int k=0; k<9; k++ ){
                int r0=k/3, c0=k%3, bit=0;
                for(int m=0; m<9; m++ ){ if( m/3!=r0 && m%3!=c0 ) bit |= (1<<m); }
                bitRC[k]=bit;
            }
		}

		public void Initialize(){
			GrpCeLKLst=new List<GroupedLink>();
		}

		public void PrepareGroupedLinkMan(){
            SearchGroupedLink();
            GrpCeLKLst.Distinct();
            GrpCeLKLst.Sort();

        //     WriteLine("GrpCeLKLst.Count:"+GrpCeLKLst.Count);
        //     int cc=0;
        //     GrpCeLKLst.ForEach(P=>{
        //         WriteLine( $"{(cc++).ToString().PadLeft(3)}:{P.ToString()}" );
        //     } );

            foreach( var P in GrpCeLKLst ){
                if( P.no!=P.no2 )  WriteLine(P);
            }
        }   
        
        private void SearchGroupedLink(){
            try{
                UGrCells[] LQ=new UGrCells[3];
                for(int no=0; no<9; no++ ){
                    int noB=1<<no;
                    Bit81 BPnoB = new Bit81(pBOARD,noB);
            
                    //------------------------------------------
                    for(int h=0; h<18; h++ ){
                        Bit81 BPnoB2 = BPnoB&pHouseCells[h];

                        List<Bit81> houseLst=new List<Bit81>();
                        List<int>   tfxLst=new List<int>();
                        for(int k=0; k<9; k++ ){
                            int hx = (h<9)? (k+9): k;
                            Bit81 BX = BPnoB2&pHouseCells[hx];
                            if( BX.IsZero() ) continue;
                            houseLst.Add(BX);
                            tfxLst.Add(hx);
                        }
                        for(int k=0; k<3; k++ ){
                            int hx = (h<9)? (h/3*3+k): ((h-9)/3+k*3);
                            hx += 18;
                            Bit81 BX = BPnoB2&pHouseCells[hx];
                            if( BX.IsZero() ) continue;
                            houseLst.Add(BX);
                            tfxLst.Add(hx);
                        }

                        if( houseLst.Count<2 ) continue;
                        Permutation prm=new Permutation(houseLst.Count,2);
                        while( prm.Successor() ){
                            int na=prm.Index[0], nb=prm.Index[1];
                            Bit81 HA=houseLst[na];
                            Bit81 HB=houseLst[nb];
                            if( (HA&HB).IsNotZero() ) continue;
                            UGrCells LA=new UGrCells(tfxLst[na],no);
                            UGrCells LB=new UGrCells(tfxLst[nb],no);
                            foreach( var P in HA.IEGetUCell_noB(pBOARD,0x1FF) ) LA.Add(P);
                            foreach( var P in HB.IEGetUCell_noB(pBOARD,0x1FF) ) LB.Add(P);
                            SetGroupedLink(tfxLst[nb],W,no,LA,LB);
                            if( (BPnoB2-(HA|HB)).IsNotZero() ) continue;
                            SetGroupedLink(tfxLst[nb],S,no,LA,LB);
                        }
                    }
           
                    //------------------------------------------
                    for(int h=18; h<27; h++ ){
                        int bx = h-18;
                        int b0 = (bx/3*27+(bx%3)*3); //Cell number at the top left of the block

                        Bit81 BPnoB2 = BPnoB&pHouseCells[h];
                        List<Bit81> houseLst=new List<Bit81>();
                        List<int>   tfxLst=new List<int>();
                        for(int k=0; k<3; k++ ){
                            int r0=(b0/9+k);
                            Bit81 BX = BPnoB2&pHouseCells[r0];
                            if( BX.IsZero() ) continue;
                            houseLst.Add(BX);
                            tfxLst.Add(r0);
                        }
                        for(int k=0; k<3; k++ ){
                            int c0=(b0%9)+k;
                            Bit81 BX = BPnoB2&pHouseCells[c0+9];
                            if( BX.IsZero() ) continue;
                            houseLst.Add(BX);
                            tfxLst.Add(c0+9);
                        }

                        if( houseLst.Count>=2 ){
                            Permutation prm=new Permutation(houseLst.Count,2);
                            while( prm.Successor() ){
                                int na=prm.Index[0], nb=prm.Index[1];
                                Bit81 HA=houseLst[na];
                                Bit81 HB=houseLst[nb]-HA;
                                if( HB.IsZero() ) continue;
                                UGrCells LA=new UGrCells(tfxLst[na],no);
                                UGrCells LB=new UGrCells(tfxLst[nb],no);
                                foreach( var P in HA.IEGetUCell_noB(pBOARD,0x1FF) ) LA.Add(P);
                                foreach( var P in HB.IEGetUCell_noB(pBOARD,0x1FF) ) LB.Add(P);
                                SetGroupedLink(tfxLst[nb],W,no,LA,LB);
                                if( (BPnoB2-(HA|HB)).IsNotZero() ) continue;
                                SetGroupedLink(tfxLst[nb],S,no,LA,LB);
                            }
                        }
                        if( houseLst.Count>=1 ){
                            for(int na=0; na<houseLst.Count; na++ ){
                                Bit81 HA=houseLst[na];
                                UGrCells LA=new UGrCells(tfxLst[na],no);
                                foreach( var P in HA.IEGetUCell_noB(pBOARD,0x1FF) ) LA.Add(P);
                                foreach( var rc in BPnoB2.IEGet_rc() ){
                                    if(HA.IsHit(rc)) continue;
                                    UGrCells LB=new UGrCells(-9,no,pBOARD[rc]);
                                    SetGroupedLink(tfxLst[na],W,no,LA,LB);
                                    SetGroupedLink(-9,W,no,LB,LA);
                                }
                            }
                        }
                    }

                }
            }
            catch( Exception ex ){
                WriteLine(ex.Message);
                WriteLine(ex.StackTrace);
            }
        }

        private  void SetGroupedLink( int h, int type, int no, UGrCells LA, UGrCells LB, bool Print=false ){
            if( LA.Count==0 || LB.Count==0 ) return;
            if( LA.Count==1 && LB.Count==1 ) return ;
            GroupedLink GrpLK = new GroupedLink(LA,LB,h,type);
            int ix = GrpCeLKLst.FindIndex(P=>(P.Equals(GrpLK)));
            if( ix>=0 ) return;
            GrpCeLKLst.Add(GrpLK);

            if( Print ){
                WriteLine( $"\r*LA h:{h} type:{type} no:{(no+1)}" );
                LA.ForEach(P=>WriteLine(P));
                WriteLine("*LB");
                LB.ForEach(P=>WriteLine(P));
            }
        } 

        public IEnumerable<GroupedLink> IEGet_GlkNoType( UGrCells GLK, int no, int typB ){
            foreach( var P in GrpCeLKLst ){
                if( P.UGCellsA==GLK )  yield return  P;
            }
            yield break;
        }
    }         
}