using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Linq;
using System.Collections;
using static System.Diagnostics.Debug;

using GIDOO_space; 

namespace GNPXcore {
    //ALS(Almost Locked Set) 
    // ALS is a state where there are "n+1 candidate digits" in "n cells" belonging to the same house. 
    // https://gidoo-code.github.io/Sudoku_Solver_Generator_v4/page26.html

  #region UALS
    public class UALS: IComparable{
        static public Bit81[]    pHouseCells{ get{ return AnalyzerBaseV2.HouseCells; } }
        static public Bit81[]    pConnectedCells{ get{ return AnalyzerBaseV2.ConnectedCells; } }

        public int               ID;
        public readonly int      Size;          //CellsSize 
        public readonly int      h;             //House Index
        public readonly int      FreeB;         //ALS element digits
        public readonly int      Level;         //FreeDigits-CellsSize
        public readonly List<UCell> UCellLst = new List<UCell>();    //ALS Cells
        private readonly int     sortkey;       
      
        public bool     singly;  //true: Even if House is different, do not register if the ALS configuration is the same.
        public int      rcbDir;                                      //bit expression of ALS
       
        public int      rcbRow{ get=> rcbDir&0x1FF; }      //row expression
        public int      rcbCol{ get=> (rcbDir>>9)&0x1FF; } //column expression
        public int      rcbBlk{ get=> (rcbDir>>18)&0x1FF; }//block expression

        public int     houseRow{ get => (rcbRow.BitCount()==1)? rcbRow.BitToNum(): -1; }
        public int     houseCol{ get => (rcbCol.BitCount()==1)? rcbCol.BitToNum()+9: -1; }
        public int     houseBlk{ get => (rcbBlk.BitCount()==1)? rcbBlk.BitToNum()+18: -1; }


        public Bit81    B81;                               //ALS expression
        public bool     ALSenabled=true;
        public Bit981   B981in;
        public Bit981   B981con;

        //working variable//(used in ALSChain)
        public bool            LimitF=false;       
        public List<UALSPair>  ConnLst;
        public List<int>       LockedNoDir;
           
        public Bit81           conectedCellsB;

        public UALS( int ID, int Size, int h, int FreeB, List<UCell> UCellLst ){
            this.ID        = ID;
            this.Size      = Size;
            this.h         = h;
            this.singly    = true;
            this.FreeB     = FreeB;
            this.Level     = FreeB.BitCount()-Size;
            this.B81       = new Bit81(UCellLst);
            this.conectedCellsB = new Bit81(UCellLst);
            this.UCellLst  = UCellLst;
            this.LockedNoDir = null;
            this.sortkey   = FreeB._SortKey();
            UCellLst.ForEach( P =>{
                rcbDir |= ( (1<<(P.b+18)) | (1<<(P.c+9)) | (1<<(P.r)) );     
                conectedCellsB |= pConnectedCells[P.rc];
            } );
        }

        public void Set_Bit981in_Bit981con( List<UCell> tBDL ){
            ALSenabled = true;
            B981in  = new Bit981(eSet:false);
            B981con = new Bit981(eSet:false);

            foreach( var no in FreeB.IEGet_BtoNo(  ) ){
                Bit81 inB = new Bit81();
                Bit81 conB = new Bit81();
                foreach( var P in UCellLst ){ 
                    inB.BPSet(P.rc);
                    conB |= pConnectedCells[P.rc];
                }
                Bit81 Q = new Bit81(tBDL,1<<no);
                B981in._BQ[no] = inB & Q;
                B981con._BQ[no] = conB & Q;
            }
            foreach( var no in FreeB.IEGet_BtoNo(  ) ){
                B981con._BQ[no] -= B81;
            }
        }

        public override int GetHashCode(){ return (B81.GetHashCode() ^ FreeB*18401 ); }
        public int CompareTo( object obj ){
            UALS UB = obj as UALS;
            if( this.Level!=UB.Level ) return (this.Level-UB.Level);
            if( this.Size!=UB.Size )   return (this.Size-UB.Size);

            if( this.FreeB!=UB.FreeB ){
                return ( this.sortkey-UB.sortkey );
            }

            int bitW = this.B81.CompareTo(UB.B81);
            if( bitW != 0 ) return bitW;
            if( this.h!=UB.h )  return (this.h-UB.h);

            return  0;
        }

        public UGrCells SelectNoCells( int no ){
            int noB=1<<no;
            List<UCell> UCsS = UCellLst.FindAll(Q=>(Q.FreeB&noB)>0);
            UGrCells GCs = new UGrCells(h,no);
            GCs.Add(UCsS);
            return GCs;
        }
        
        public bool IsPureALS(){
            //Pure ALS : not include locked set of proper subset
            if( Size<=2 ) return true;
            for( int sz=2; sz<Size-1; sz++ ){
                var cmb=new Combination(Size,sz);
                while(cmb.Successor()){
                    int fb=0;
                    for(int k=0; k<sz; k++ )  fb |= UCellLst[cmb.Index[k]].FreeB;
                    if( fb.BitCount()==sz ) return false;
                }
            }
            return true;
        }

        public override string ToString(){
            string st = $"<> UALS {ID} <>  h:{h} Size:{Size} Level:{Level} ";
            st += $" singly:{singly}{(singly? "*":"")}";
            st += $" NoB:{FreeB.ToBitString(9)}\r";
            st += $"         B81 {B81}\r";
            for(int k=0; k<UCellLst.Count; k++){
                st += "------";
                int rcW = UCellLst[k].rc;
                st += $" rc:{rcW.ToRCString()}";
                st += $" FreeB:{UCellLst[k].FreeB.ToBitString(9)}";
                st += $" rcb:B{(rcbBlk).ToBitString(9)}";
                st += $" c{rcbCol.ToBitString(9)}";
                st += $" r{rcbRow.ToBitString(9)}";
                st += $" conectedCellsB:{conectedCellsB.ToString()}";
                st += "\r";
            }
            return st;
        }

        public string ToStringRCN(){
            string st="";
            UCellLst.ForEach( p =>{  st += $" {p.rc.ToRCString()}"; } );
            st = st.ToString_SameHouseComp()+" #"+FreeB.ToBitStringN(9);
            return st;
        }
        public string ToStringRC(){
            string st="";
            UCellLst.ForEach( p =>{  st += $" {p.rc.ToRCString()}"; } );
            st = st.ToString_SameHouseComp();
            return st;
        }
    }
  #endregion UALS
 
  //The following two classes will be developed collectively into GroupedLink (will do so)
  #region UALSPair
    public class UALSPair{
        public readonly UALS ALSpre;
        public readonly UALS ALSnxt;
        public readonly int  RCC;       //Restricted Common Candidate
        public readonly int  nRCC=-1;   //no:0...8(In the case of doubly, make links individually)
        public Bit81         rcUsed;
        public UALSPair( UALS ALSpre, UALS ALSnxt, int RCC, int nRCC, Bit81 B81rc=null ){
            this.ALSpre=ALSpre; this.ALSnxt=ALSnxt; this.RCC=RCC; this.nRCC=nRCC;
            this.rcUsed = B81rc?? (ALSpre.B81|ALSnxt.B81);
        }
        public  override bool Equals( object obj ){
            var A = obj as UALSPair;
            if( A.nRCC        !=nRCC )         return false;
            if( A.ALSpre.Size !=ALSpre.Size )  return false;
            if( A.ALSnxt.Size !=ALSnxt.Size )  return false; 
            if( A.ALSpre.FreeB!=ALSpre.FreeB ) return false;
            if( A.ALSnxt.FreeB!=ALSnxt.FreeB ) return false;
            if( A.ALSpre.B81!=ALSpre.B81 ) return false;
            if( A.ALSnxt.B81!=ALSnxt.B81 ) return false;
            return true;
        }
        public override int GetHashCode(){ return base.GetHashCode(); }
    }
  #endregion UALSPair

  #region LinkCellALS
        // Cell-ALS Link
    public class LinkCellALS: IComparable{
        static public Bit81[]    pConnectedCells{ get{ return AnalyzerBaseV2.ConnectedCells; } }
        public readonly UCell UC;
        public readonly UALS  ALS;
        public readonly int   nRCC=-1; //no:0..8 (not bit representation)

        public int            noBcand;

        public LinkCellALS( UCell UC, UALS ALS, int nRCC ){
            this.UC=UC; this.ALS=ALS; this.nRCC=nRCC;
        }
        public  override bool Equals( object obj ){
            var A = obj as LinkCellALS;
            return (this.ALS.ID==A.ALS.ID);
        }

        public int CompareTo( object obj ){
            LinkCellALS A = obj as LinkCellALS;
            return (this.ALS.ID-A.ALS.ID);
        }
        public int CompareTo( ){
            return (this.ALS.ID);
        }

        public override string ToString(){
            string st = $"LinkCellALS nRCC:#{nRCC+1}";
            st += " UCell:"+UC+"\rALS:"+ ALS;
            return st;
        }
        public override int GetHashCode(){ return base.GetHashCode(); }

        public Bit981 Func_Connected_ALS2Cell( Bit981 BP981, int rcX, int noX ){
                
            //ALS turns to LockedSet if cell[rcX]#noX is true.        
            if( !(ALS.B981in._BQ[noX] is null) ){ 
                if( (ALS.B981in._BQ[noX]-pConnectedCells[rcX]).IsNotZero() ) return null;
                //All digit #noX in ALS are false, then ALS turns to LockedSet.
            }   

            Bit981 Locked981 = new Bit981(eSet:false);
            bool hitB=false;
            foreach( var noA in ALS.FreeB.IEGet_BtoNo() ){
                int noAB = 1<<noA;
                Bit81 B=null;
                if( noA==noX ) B = pConnectedCells[rcX];
                else{
                    B = new Bit81(all1:true);
                    foreach( var UC in ALS.UCellLst ){
                        if( (UC.FreeB&noAB) > 0 )  B &= pConnectedCells[UC.rc];
                    }
                }
                B &= BP981._BQ[noA];
                if( B.IsNotZero() ){ hitB=true; Locked981._BQ[noA]=B; }
            }
            if( !hitB )  return null;

            return Locked981;
        }

    }
  #endregion LinkCellALS
}
