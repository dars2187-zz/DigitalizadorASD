   M S B A M L   `   `   `  ����H  DDigitalizador, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullC&clr-namespace:Digitalizador.ViewModelsDigitalizador.ViewModels  5   	       Digitalizador.MainWindow   5   	   W SPresentationCore, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35e aSystem.Windows.Controls.Ribbon, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089R NWindowsBase, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=5.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5   	   R NSystem.Xaml, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   8local&clr-namespace:Digitalizador.ViewModels     5   	   -   5          �Name$  	theWindow��5   	    � Title$ 
MainWindow��5	   	   $	��450��6   $	��800��6)   .��5
      �  � ColumnDefinitions 5   
   �� 5      $
��Auto �6   �� 5      $��* �6    � RowDefinitions 5   
   �� 5      $��* �6   �� 5      $
��Auto �6   .+�5   
   �� $
��Left=�6   $��0��6.   $��0��6;   $	��Top=�5      $	��120��6+    �� DisplayMemberPath$
 Name��5   3    �� SelectedItem 5      �� *CurrentDevice+ �� Mode$ TwoWay=���5      �� *	Devices+63   �� 5   
   $��0��6   $��1��6   $��2��6/   $��Disabled=�5       �� SelectionMode$ Extended=�5      ��5   @   �� *Pages+��5      �� 5      .R�5      � 5      ��5      �� 5       .R�5!      ��  �� Padding$ 4q�6   ._�5"      �� $��Uniform=�6    	 �� BitmapScalingMode$	 
LowQuality=�5#       ��5$       �� 
 �� ElementName$
 	theWindow�� �� Path$ DataContext.ThumbnailSize���5%       �� $
 	theWindow��$ DataContext.ThumbnailSize���5&       �� 65   5'      5(      ��5*      �� 5+       �� 
TargetType" ��6    �� BasedOn# [B��6:   .�5,      -   5        5-      53      �� 54   
   $
��Scan��6   $��1��6e   $��0��6r   ��6    �� *CaptureCommand+{�6C   �� *EnabledScan+6�   55      56      