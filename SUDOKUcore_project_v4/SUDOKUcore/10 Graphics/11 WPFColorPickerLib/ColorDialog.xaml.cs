﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace WPFColorPickerLib{
    public partial class ColorDialog : Window{
        #region Ctor
        public ColorDialog(){
            InitializeComponent();
        }
   
        public ColorDialog( Color StartColor ){
            InitializeComponent();
            colorPicker.StartColor = StartColor;
        }
        #endregion

        #region Public Properties
        public Color SelectedColor{ get=>colorPicker.SelectedColor; }
        #endregion

        #region Private Methods
        /// <summary>
        /// Closes the dialog on Enter key pressed
        /// </summary>
        private void Window_KeyDown(object sender, KeyEventArgs e ){
            if( e.Key==Key.Enter) this.Close();
        }

        /// <summary>
        /// User is happy with choice
        /// </summary>
        private void btnOk_Click(object sender, RoutedEventArgs e ){
            DialogResult = true;
        }

        /// <summary>
        /// User is not happy with choice
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e){
            DialogResult = false;
        }
        #endregion
    }
}
