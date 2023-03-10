using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Globalization;

using static System.Math;
using static System.Diagnostics.Debug;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

using Microsoft.Win32;

using System.Diagnostics;
using System.Threading;
using System.Windows.Threading;
using System.Threading.Tasks;

using GIDOOCV;

using GIDOO_space;

namespace GNPXcore{
    using pRes=Properties.Resources;
    using sysWin=System.Windows;

    public partial class NuPz_Win{   
        private string    fNameSDK;

        // btnOpenPuzzleFile_Click
        private void btnOpenPuzzleFile_Click( object sender, RoutedEventArgs e ){
            var OpenFDlog = new OpenFileDialog();
            OpenFDlog.Multiselect = false;

            OpenFDlog.Title  = pRes.filePuzzleFile;
            OpenFDlog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            if( (bool)OpenFDlog.ShowDialog() ){
                fNameSDK = OpenFDlog.FileName;
                GNPX_000.SDK_FileInput( fNameSDK, (bool)chbInitialState.IsChecked );
                txtFileName.Text = fNameSDK;

                _SetScreenProblem();
                GNPX_000._SDK_Ctrl_Initialize();

                btnProbPre.IsEnabled = (GNPX_000.CurrentPrbNo>=1);
                btnProbNxt.IsEnabled = (GNPX_000.CurrentPrbNo<GNPX_000.SDKProbLst.Count-1);
            }
        }





        // btnSavePuzzle_Click
        private void btnSavePuzzle_Click( object sender, RoutedEventArgs e ){
            var SaveFDlog = new SaveFileDialog();
            SaveFDlog.Title  =  pRes.filePuzzleFile;
            SaveFDlog.FileName = fNameSDK;
            SaveFDlog.Filter = "txt files (*.txt)|*.txt|All files (*.*)|*.*";
            
            GNPX_App.SlvMtdCList[0] = true;
            if( !(bool)SaveFDlog.ShowDialog() ) return;
            fNameSDK = SaveFDlog.FileName;
            bool append  = (bool)chbAdditionalSave.IsChecked;
            bool fType81 = (bool)chbFile81Nocsv.IsChecked;
            bool blank9  = (bool)chbFile81Plus.IsChecked;
            bool SolSort = (bool)chbSolutionSort.IsChecked;
            bool SolSet  = (bool)cbxProbSolSetOutput.IsChecked;
            bool SolSet2 = (bool)chbAddAlgorithmList.IsChecked;

            if( GNPX_000.SDKProbLst.Count==0 ){
                if( pGP.BDL.All(p=>p.No==0) ) return;
                pGP.ID = GNPX_000.SDKProbLst.Count;
                GNPX_000.SDKProbLst.Add(pGP);
                GNPX_000.CurrentPrbNo=0;
                _SetScreenProblem();
            }
            GNPX_000.pGNPX_Eng.Set_Methods_for_Solving(AllMthd:false);  //true:All Method 
            GNPX_000.SDK_FileOutput( fNameSDK, append, fType81, SolSort, SolSet, SolSet2, blank9 );
        }





        // btnSaveToFavorites_Click
        private void btnSaveToFavorites_Click( object sender, RoutedEventArgs e ){
            GNPX_000.btnFavfileOutput(true,SolSet:true,SolSet2:true);
        }


        // cbxProbSolSetOutput_Checked
        private void cbxProbSolSetOutput_Checked( object sender, RoutedEventArgs e ){
            chbAddAlgorithmList.IsEnabled = (bool)cbxProbSolSetOutput.IsChecked;
            Color cr = chbAddAlgorithmList.IsEnabled? Colors.White: Colors.Gray;
            chbAddAlgorithmList.Foreground = new SolidColorBrush(cr); 
        }

        private void chbFile81Nocsv_Checked(object sender, RoutedEventArgs e) {
            if( chbFile81Plus is null )  return;
            Color cr = (bool)chbFile81Nocsv.IsChecked? Colors.White: Colors.Gray;
            chbFile81Plus.Foreground = new SolidColorBrush(cr); 
        }


        //Copy/Paste Puzzle(board<-->clipboard)
        private void Grid_PreviewKeyDown( object sender, KeyEventArgs e ){
            bool KeySft  = (Keyboard.Modifiers&ModifierKeys.Shift)>0;
            bool KeyCtrl = (Keyboard.Modifiers&ModifierKeys.Control)>0;

            var P=Mouse.GetPosition(PB_GBoard);
            var (W,H)=(PB_GBoard.Width,PB_GBoard.Height);
            if(P.X<0 || P.Y<0 || P.X>=W || P.Y>=H)  return;



            if( e.Key==Key.C && KeyCtrl ){                // Ctrl+"c" => board -> Clipboard(81"n")
                string st=pGP.CopyToBuffer( KeySft:KeySft );
                try{
                    Clipboard.Clear();
                    Clipboard.SetData(DataFormats.Text, st);
                }
                catch(System.Runtime.InteropServices.COMException){ /* NOP */ }
            }



            else if( e.Key==Key.F && KeyCtrl ){           // Ctrl+"f" => board -> Clipboard(9x9"n")
                string st=pGP.ToGridString(KeySft);   
                try{
                    Clipboard.Clear();
                    Clipboard.SetData(DataFormats.Text, st);
                }
                catch(System.Runtime.InteropServices.COMException){ /* NOP */ }
            }



            else if( e.Key==Key.V && KeyCtrl ){           // Ctrl+"v" => Clipboard(any foramt) -> board
                string st=(string)Clipboard.GetData(DataFormats.Text);
                Clipboard.Clear();
                if( st==null || st.Length<81 ) return ;
                var UP=GNPX_000.SDK_ToUPuzzle(st,saveF:true); 
                if( UP==null) return;
                GNPX_000.CurrentPrbNo=999999999;//GNPX_000.SDKProbLst.Count-1
                _SetScreenProblem();
                _ResetAnalyzer(false); //Clear analysis result

            }

        }



    }
}