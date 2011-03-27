/*
 * Copyright 2004,2006 The Poderosa Project.
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 *
 * $Id: MainWindow.cs,v 1.2 2010/12/18 09:40:56 kzmi Exp $
 */
using System;
using System.Collections.Generic;
using System.Collections;
using System.Resources;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using System.Text.RegularExpressions;

using Poderosa.Util;
using Poderosa.Plugins;
using Poderosa.Boot;
using Poderosa.Document;
using Poderosa.Sessions;
using Poderosa.Commands;
using Poderosa.UI;
using Poderosa.View;
using Poderosa.Util.Collections;

namespace Poderosa.Forms
{
    //���C���E�B���h�E
    internal class MainWindow : PoderosaForm, IPoderosaMainWindow {

        private IViewManager _viewManager;
        private MainWindowArgument _argument;
        private MenuStrip _mainMenu;
        private TabBarTable _tabBarTable;
        private PoderosaToolStripContainer _toolStripContainer;
        private PoderosaStatusBar _statusBar;
        private TabBarManager _tabBarManager;

        public MainWindow(MainWindowArgument arg, MainWindowMenu menu) {
            _argument = arg;
            Debug.Assert(_argument!=null);
            _commandKeyHandler.AddLastHandler(new FixedShortcutKeyHandler(this));

            this.ImeMode = ImeMode.NoControl;
#if MONOLITHICLIBRARY
            this.AllowDrop=false;
#else
            this.AllowDrop = true;
#endif
            arg.ApplyToUnloadedWindow(this);

            InitContent();

            ReloadMenu(menu, true);
        }

        private void InitContent() {
            this.SuspendLayout();

            IExtensionPoint creator_ext = WindowManagerPlugin.Instance.PoderosaWorld.PluginManager.FindExtensionPoint(WindowManagerConstants.MAINWINDOWCONTENT_ID);
            IViewManagerFactory f = ((IViewManagerFactory[])creator_ext.GetExtensions())[0];

            _toolStripContainer = new PoderosaToolStripContainer(this, _argument.ToolBarInfo);

#if MONOLITHICLIBRARY
            _toolStripContainer.TopToolStripPanelVisible = false;
#endif

            this.Controls.Add(_toolStripContainer);
                
            //�X�e�[�^�X�o�[���̑��̏�����
            //�R���g���[����ǉ����鏇�Ԃ͏d�v�I
            _viewManager = f.Create(this);
            Control main = _viewManager.RootControl;
#if MONOLITHICLIBRARY
            //main.Dock = DockStyle.Fill;
            //_toolStripContainer.ContentPanel.Controls.Add(main);
            _viewManager.RootControl.Visible=false;
#else
#endif
            if (main!=null) { //�e�X�g�P�[�X�ł̓E�B���h�E�̒��g���Ȃ����Ƃ�����
                main.Dock = DockStyle.Fill;
                _toolStripContainer.ContentPanel.Controls.Add(main);
            }

            int rowcount = _argument.TabRowCount;
            _tabBarTable = new TabBarTable();
            _tabBarTable.Height = rowcount * TabBarTable.ROW_HEIGHT; 
            _tabBarTable.Dock = DockStyle.Top;
            _tabBarManager = new TabBarManager(_tabBarTable);

            _statusBar = new PoderosaStatusBar();

#if !MONOLITHICLIBRARY
            _toolStripContainer.ContentPanel.Controls.Add(_tabBarTable);            
#else
            _statusBar.Visible = false;
#endif
            this.Controls.Add(_statusBar); //�����łȂ��A_toolStripContainer.BottomToolStripPanel��_statusBar��ǉ����Ă��悳���������A��������ƃc�[���o�[���ڂ��X�e�[�^�X�o�[�̏㉺�ɑ}���\�ɂȂ��Ă��܂�
            _tabBarTable.Create(rowcount);

            this.ResumeLayout();
        }

        public PoderosaToolStripContainer ToolBarInternal {
            get {
                return _toolStripContainer;
            }
        }

        #region IPoderosaMainWindow & IPoderosaForm
        public IViewManager ViewManager {
            get {
                return _viewManager;
            }
        }
        public IDocumentTabFeature DocumentTabFeature {
            get {
                return _tabBarManager;
            }
        }
        public IContentReplaceableView LastActivatedView {
            get {
                IPoderosaDocument doc = _tabBarManager.ActiveDocument;
                if(doc==null)
                    return null;
                else
                    return SessionManagerPlugin.Instance.FindDocumentHost(doc).LastAttachedView as IContentReplaceableView;
            }
        }
        public IToolBar ToolBar {
            get {
                return _toolStripContainer;
            }
        }
        public IPoderosaStatusBar StatusBar {
            get {
                return _statusBar;
            }
        }

        #endregion


        protected override void OnLoad(EventArgs e) {
            //NOTE �Ȃ����͕s�������A�E�B���h�E�̈ʒu��Form.Show()�̌Ăяo���O�Ɏw�肵�Ă���������ēK���Ȉʒu���ݒ肳��Ă��܂��B
            //�Ȃ̂ł����ōs���悤�ɂ����B
            _argument.ApplyToLoadedWindow(this);
            base.OnLoad(e);
            //�ʒm �N���[�Y����WindowManager���o�^����C�x���g�n���h������
            WindowManagerPlugin.Instance.NotifyMainWindowLoaded(this);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e) {
            base.OnClosing(e);
            try {
                if(SessionManagerPlugin.Instance==null) return; //�P�̃e�X�g�ł�Session�Ȃ��ŋN�����邱�Ƃ����肾

                CommandResult r = CommandManagerPlugin.Instance.Execute(BasicCommandImplementation.CloseAll, this);
                if(r==CommandResult.Cancelled) {
                    _closeCancelled = true;
                    e.Cancel = true;
                }
                else
                    e.Cancel = false;
            }
            catch(Exception ex) {
                RuntimeUtil.ReportException(ex);
                e.Cancel = false; //�o�O�̂��߂ɃE�B���h�E����邱�Ƃ��ł��Ȃ��A�Ƃ����̂͂܂���
            }
        }

    

        public void ReloadMenu(MainWindowMenu menu, bool with_toolbar) {
            this.SuspendLayout();
            if(_mainMenu!=null) this.Controls.Remove(_mainMenu);
            _mainMenu = new MenuStrip();
            menu.FullBuild(_mainMenu, this);
            this.MainMenuStrip = _mainMenu;
#if MONOLITHICLIBRARY
            this.MainMenuStrip.Visible = false;
#endif
            this.Controls.Add(_mainMenu);
            
            if(with_toolbar && _toolStripContainer!=null)
                _toolStripContainer.Reload();

            this.ResumeLayout();
        }
        public void ReloadPreference(ICoreServicePreference pref) {
            IPoderosaAboutBoxFactory af = AboutBoxUtil.GetCurrentAboutBoxFactory();
            if (af != null)
                this.Icon = af.ApplicationIcon;
            _toolStripContainer.ReloadPreference(pref);
        }

        protected override void OnDragEnter(DragEventArgs args)
        {
#if DISABLEDRAGDROP
            base.OnDragEnter(args);
            try {
                WindowManagerPlugin.Instance.BypassDragEnter(this, args);
            }
            catch(Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
#endif
        }
        protected override void OnDragDrop(DragEventArgs args) {
#if DISABLEDRAGDROP
            base.OnDragDrop(args);
            try {
                WindowManagerPlugin.Instance.BypassDragDrop(this, args);
            }
            catch(Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
#endif
        }
        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);
            if(m.Msg==Win32.WM_COPYDATA) {
                unsafe {
                    Win32.COPYDATASTRUCT* p = (Win32.COPYDATASTRUCT*)m.LParam.ToPointer();
                    if(p!=null && p->dwData==Win32.PODEROSA_OPEN_FILE_REQUEST) {
                        string fn = new String((char*)p->lpData);
                        m.Result = new IntPtr(Win32.PODEROSA_OPEN_FILE_OK);
                        WindowManagerPlugin.Instance.TurningOpenFile(this, fn);
                    }
                }
            }
        }
    }

    internal class TabBarManager : IDocumentTabFeature, TabBarTable.IUIHandler {
        private TabBarTable _tabBarTable;

        public TabBarManager(TabBarTable table) {
            _tabBarTable = table;
            _tabBarTable.AllowDrop = true;
            _tabBarTable.UIHandler = this;
        }

        public IPoderosaDocument[] GetHostedDocuments() {
            return KeysToDocuments(_tabBarTable.GetAllDocuments());
        }

        #region IDocumentTabFeature
        public IPoderosaDocument ActiveDocument {
            get {
                return KeyToDocument(_tabBarTable.ActiveTabKey);
            }
        }
        public IPoderosaDocument GetAtOrNull(int index) {
            return KeyToDocument(_tabBarTable.GetAtOrNull(index));
        }
        public int IndexOf(IPoderosaDocument document) {
            return _tabBarTable.IndexOf(DocumentToKey(document));
        }
        public int DocumentCount {
            get {
                return _tabBarTable.TabCount;
            }
        }

        public void Add(IPoderosaDocument document) {
            TabBarUpdateState state = TabBarUpdateState.BeginUpdate("addtab");
            _tabBarTable.AddTab(DocumentToKey(document));
            state.Commit();
        }

        public void Remove(IPoderosaDocument document) {
            TabBarUpdateState state = TabBarUpdateState.BeginUpdate("removetab");
            _tabBarTable.RemoveTab(DocumentToKey(document), false);
            state.Commit();
        }

        public void Update(IPoderosaDocument document) {
            if(_tabBarTable.InvokeRequired) {
                _tabBarTable.Invoke(new UpdateDelegate(Update), document);
                return;
            }

            TabBarUpdateState state = TabBarUpdateState.BeginUpdate("updatetab");
            _tabBarTable.UpdateDescription(DocumentToKey(document));
            state.Commit();

            //�C�x���g�����ʒm����΂����̂ł�����Ɖߏ�ȏ�������
            if(document==this.ActiveDocument)
                SessionManagerPlugin.Instance.ActivateDocument(document, ActivateReason.InternalAction);
        }
        private delegate void UpdateDelegate(IPoderosaDocument document);

        //SessionManager����̂݌ĂԂ���
        public void Activate(IPoderosaDocument document) {
#if DEBUG
            Debug.Assert(document==null || _tabBarTable.ContainsKey(DocumentToKey(document)));
#endif
            if(document==KeyToDocument(_tabBarTable.ActiveTabKey)) return; //do nothing

            TabBarUpdateState state = TabBarUpdateState.BeginUpdate("activate");
            if(document==null)
                _tabBarTable.Deactivate(false);
            else
                _tabBarTable.Activate(DocumentToKey(document), false);
          
            state.Commit();
        }

        public int TabRowCount {
            get {
                return _tabBarTable.TabBarCount; //Controls.Count�ɂ���ƁA�I������preference�ɋL�^���邱�Ƃ����܂��ł��Ȃ�
            }
        }
        public void SetTabRowCount(int count) {
            _tabBarTable.SetTabBarCount(count);
        }

        public IAdaptable GetAdapter(Type adapter) {
            return WindowManagerPlugin.Instance.PoderosaWorld.AdapterManager.GetAdapter(this, adapter);
        }

        #endregion

        #region TabBarTable.IUIHandler
        public void ActivateTab(TabKey key) {
            SessionManagerPlugin.Instance.ActivateDocument(KeyToDocument(key), ActivateReason.TabClick);
        }
        public void MouseMiddleButton(TabKey key) {
            IPoderosaDocument doc = KeyToDocument(key);
            SessionManagerPlugin sm = SessionManagerPlugin.Instance;

            bool was_active = _tabBarTable.ActiveTabKey==key;
            IPoderosaView view = sm.FindDocumentHost(doc).LastAttachedView;
            sm.CloseDocument(doc);

            //�A�N�e�B�u�Ȃ��������
            if(was_active && view!=null && view.Document!=null) {
                sm.ActivateDocument(view.Document, ActivateReason.InternalAction);
            }
        }
        public void MouseRightButton(TabKey key) {
            IPoderosaDocument doc = KeyToDocument(key);
            IPoderosaContextMenuPoint ctx_pt = (IPoderosaContextMenuPoint)doc.GetAdapter(typeof(IPoderosaContextMenuPoint));

            //���j���[�����Ȃ��ꍇ�͖���
            if(ctx_pt==null || ctx_pt.ContextMenu==null || ctx_pt.ContextMenu.Length==0) return;

            IPoderosaForm f = (IPoderosaForm)_tabBarTable.ParentForm;
            f.ShowContextMenu(ctx_pt.ContextMenu, doc, Control.MousePosition, ContextMenuFlags.None);
        }
        public void StartTabDrag(TabKey key) {
            WindowManagerPlugin.Instance.SetDraggingTabBar(key);
        }
        public void AllocateTabToControl(TabKey key, Control target) {
            IAdaptable ad = target as IAdaptable;
            if(ad==null) return;

            IPoderosaView view = (IPoderosaView)ad.GetAdapter(typeof(IPoderosaView));
            if(view==null) return;

            SessionManagerPlugin.Instance.AttachDocumentAndView(KeyToDocument(key), view);
        }
        public void BypassDragEnter(DragEventArgs args) {
            try {
                WindowManagerPlugin.Instance.BypassDragEnter(_tabBarTable.ParentForm, args);
            }
            catch(Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }
        public void BypassDragDrop(DragEventArgs args) {
            try {
                WindowManagerPlugin.Instance.BypassDragDrop(_tabBarTable.ParentForm, args);
            }
            catch(Exception ex) {
                RuntimeUtil.ReportException(ex);
            }
        }
        #endregion


        private static IPoderosaDocument KeyToDocument(TabKey key) {
            if(key==null) return null;
            Debug.Assert(key is InternalTabKey);
            return ((InternalTabKey)key).PoderosaDocument;
        }
        private static TabKey DocumentToKey(IPoderosaDocument doc) {
            return new InternalTabKey(doc);
        }
        private static IPoderosaDocument[] KeysToDocuments(TabKey[] keys) {
            IPoderosaDocument[] r = new IPoderosaDocument[keys.Length];
            for(int i=0; i<r.Length; i++)
                r[i] = KeyToDocument(keys[i]);
            return r;
        }

        //TabKey
        public class InternalTabKey : TabKey {
            private IPoderosaDocument _document;
            public InternalTabKey(IPoderosaDocument doc) : base(doc) {
                _document = doc;
            }
            public override string Caption {
                get {
                    return _document.Caption;
                }
            }
            public override Image Icon {
                get {
                    return _document.Icon;
                }
            }

            public IPoderosaDocument PoderosaDocument {
                get {
                    return _document;
                }
            }
        }
    }

    internal class CommandShortcutKeyHandler : IKeyHandler {
        private PoderosaForm _window;

        public CommandShortcutKeyHandler(PoderosaForm window) {
            _window = window;
        }

        public UIHandleResult OnKeyProcess(Keys key) {
            IGeneralCommand cmd = CommandManagerPlugin.Instance.Find(key);
            if(cmd!=null) {
                try {
                    if(cmd.CanExecute(_window))
                        CommandManagerPlugin.Instance.Execute(cmd, _window);
                    return UIHandleResult.Stop; //�L�[�����蓖�Ă��Ă���Ύ��s���ł��邩�ǂ����ɂ�����炸Stop�B�łȂ���Alt�L�[����݂̂Ƃ����j���[�Ƀt�H�[�J�X���D���Ă��܂�
                }
                catch(Exception ex) {
                    RuntimeUtil.ReportException(ex);
                }
            }
            return UIHandleResult.Pass;
        }

        public string Name {
            get {
                return "shortcut-key";
            }
        }
    }

    //Alt+<n>, Ctrl+Tab�ȂǁA�J�X�^�}�C�Y�s�̓��������
    internal class FixedShortcutKeyHandler : IKeyHandler {
        private MainWindow _window;

        public FixedShortcutKeyHandler(MainWindow window) {
            _window = window;
        }

        public UIHandleResult OnKeyProcess(Keys key) {
            Keys modifier = key & Keys.Modifiers;
            Keys body     = key & Keys.KeyCode;
            int n = (int)body - (int)Keys.D1;
            if(modifier==Keys.Alt && n>=0 && n<=8) { //�P����X�̃L�[��#0����#8�܂łɑΉ�����
                IPoderosaDocument doc = _window.DocumentTabFeature.GetAtOrNull(n);
                if(doc!=null) {
                    SessionManagerPlugin.Instance.ActivateDocument(doc, ActivateReason.InternalAction);
                    return UIHandleResult.Stop;
                }
            }
            else if(body==Keys.Tab && (modifier==Keys.Control || modifier==(Keys.Control|Keys.Shift))) { //Ctrl+Tab, Ctrl+Shift+Tab
                IPoderosaDocument doc = _window.DocumentTabFeature.ActiveDocument;
                //�h�L�������g�͂��邪�A�N�e�B�u�Ȃ�͂Ȃ��A�Ƃ�����Ԃ�����
                int count = _window.DocumentTabFeature.DocumentCount;
                if(count > 0) {
                    int index = doc==null? -1 : _window.DocumentTabFeature.IndexOf(doc); //doc��null�̂Ƃ��͕ʈ���

                    if(modifier==Keys.Control)
                        index = (doc==null || index==count-1)? 0 : index+1;
                    else
                        index = (doc==null || index==0)? count-1 : index-1;

                    SessionManagerPlugin.Instance.ActivateDocument(_window.DocumentTabFeature.GetAtOrNull(index), ActivateReason.InternalAction);
                    return UIHandleResult.Stop;
                }
            }

            return UIHandleResult.Pass;
        }

        public string Name {
            get {
                return "fixed-key";
            }
        }
    }

    internal class WindowCaptionManager : IActiveDocumentChangeListener {
        public void OnDocumentActivated(IPoderosaMainWindow window, IPoderosaDocument document) {
            window.AsForm().Text = String.Format("{0} - Poderosa", document.Caption);
        }

        public void OnDocumentDeactivated(IPoderosaMainWindow window) {
            window.AsForm().Text = "Poderosa";
        }
    }
}
