using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ZOOM_SDK_DOTNET_WRAP;

namespace ZoomWinForms
{
    public partial class ZoomForm : Form
    {
        #region Constants
        const string CONST_APP_KEY = ""; //set your app key here
        const string CONST_APP_SECRET = ""; //set your app secret here

        const ulong CONST_MEETING_NUMBER = 0; // meeting or webinar#
        const string CONST_USER_NAME = "TestUser";
        const string CONST_MEETING_PASSWORD = ""; // meeting or webinar pwd
        const string CONST_WEBINAR_EMAIL = "Test@email.com"; //email for webinar
        #endregion Constants

        public ZoomForm()
        {
            InitializeComponent();
        }

        private void ZoomForm_Load(object sender, EventArgs e)
        {
            InitParam param = new InitParam();
            param.web_domain = "https://zoom.us";
            param.config_opts = new ConfigurableOptions() { optionalFeatures = 1 << 5 };
            //param.enable_log = true;
            if(CZoomSDKeDotNetWrap.Instance.Initialize(param) != SDKError.SDKERR_SUCCESS)
                ShowMessageAndCloseApp("Invalid Zoom initialize");

            //TODO: Check is it need?
            ISettingServiceDotNetWrap settings = CZoomSDKeDotNetWrap.Instance.GetSettingServiceWrap();
            IAudioSettingContextDotNetWrap audioSett = settings.GetAudioSettings();
            audioSett.EnableAlwaysMuteMicWhenJoinVoip(true);
            audioSett.EnableAutoJoinAudio(true);

            CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onAuthenticationReturn(onAuthenticationReturn);
            CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onLoginRet(onLoginRet);
            CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().Add_CB_onLogout(onLogout);

            if(string.IsNullOrEmpty(CONST_APP_KEY))
                ShowMessageAndCloseApp("Invalid app key");

            if (string.IsNullOrEmpty(CONST_APP_SECRET))
                ShowMessageAndCloseApp("Invalid app secret");

            AuthParam authparam = new ZOOM_SDK_DOTNET_WRAP.AuthParam();
            authparam.appKey = CONST_APP_KEY;
            authparam.appSecret = CONST_APP_SECRET;
            if (CZoomSDKeDotNetWrap.Instance.GetAuthServiceWrap().SDKAuth(authparam) != SDKError.SDKERR_SUCCESS)
                ShowMessageAndCloseApp("Invalid auth params");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //clean up sdk
            CZoomSDKeDotNetWrap.Instance.CleanUp();
            base.OnClosing(e);
        }

        #region Auth
        public void onAuthenticationReturn(AuthResult ret)
        {
            if (ret != AuthResult.AUTHRET_SUCCESS)
                ShowMessageAndCloseApp("Invalid auth");

            Connect();
        }

        public void onLoginRet(LOGINSTATUS ret, IAccountInfo pAccountInfo)
        {
            //MessageBox.Show("onLoginRet: " + ret.ToString());
        }
        public void onLogout()
        {
            //MessageBox.Show("onLogout");
        }
        #endregion Auth

        #region Connect
        public void onMeetingStatusChanged(MeetingStatus status, int iResult)
        {
            switch (status)
            {
                case MeetingStatus.MEETING_STATUS_INMEETING:
                    //MessageBox.Show("Connected!");
                    DisplayVideo();
                    DisplayShareScreen();
                    ListenAudio();
                    UpdateContainers();
                    break;

                case MeetingStatus.MEETING_STATUS_ENDED:
                case MeetingStatus.MEETING_STATUS_FAILED:
                    ShowMessageAndCloseApp("Invalid meeting status: " + status);
                    break;
                default://todo
                    break;
            }
        }

        public void onUserJoin(Array lstUserID)
        {
            if (null == (Object)lstUserID)
                return;

            for (int i = lstUserID.GetLowerBound(0); i <= lstUserID.GetUpperBound(0); i++)
            {
                UInt32 userid = (UInt32)lstUserID.GetValue(i);
                ZOOM_SDK_DOTNET_WRAP.IUserInfoDotNetWrap user = ZOOM_SDK_DOTNET_WRAP.CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().
                    GetMeetingParticipantsController().GetUserByUserID(userid);
                if (null != (Object)user)
                {
                    string name = user.GetUserNameW();
                    Console.Write(name);
                }
            }
        }

        public void onUserLeft(Array lstUserID)
        {
            //todo
        }

        public void onHostChangeNotification(UInt32 userId)
        {
            //todo
        }

        public void onLowOrRaiseHandStatusChanged(bool bLow, UInt32 userid)
        {
            //todo
        }

        public void onUserNameChanged(UInt32 userId, string userName)
        {
            //todo
        }

        public void InputMeetingPasswordAndScreenNameNotification(IMeetingPasswordAndScreenNameHandler pHandler)
        {
            MessageBox.Show("InputMeetingPasswordAndScreenNameNotification");
        }

        public void WebinarNeedRegisterNotification(IWebinarNeedRegisterHandler pHandler)
        {
            WebinarNeedRegisterType registerType = pHandler.GetWebinarNeedRegisterType();

            if(registerType == WebinarNeedRegisterType.WebinarReg_By_Email_and_DisplayName)
            {
                IWebinarNeedRegisterHandlerByEmail byEmail = pHandler as IWebinarNeedRegisterHandlerByEmail;
                byEmail.InputWebinarRegisterEmailAndScreenName(CONST_WEBINAR_EMAIL, CONST_USER_NAME);
            }
            else if(registerType == WebinarNeedRegisterType.WebinarReg_By_Register_Url)
            {
                MessageBox.Show("WebinarReg_By_Register_Url");
            }
            else
            {
                MessageBox.Show("Unknown WebinarNeedRegisterType");
            }
        }

        private void RegisterCallBack()
        {
            IMeetingServiceDotNetWrap meetingService = CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap();
            meetingService.Add_CB_onMeetingStatusChanged(onMeetingStatusChanged);

            var meetingConfiguration = meetingService.GetMeetingConfiguration();
            //meetingConfiguration.RedirectWebinarNeedRegister(false);
            meetingConfiguration.PrePopulateWebinarRegistrationInfo("serghdt@gmail.com", "serg");

            meetingConfiguration.Add_CB_onInputMeetingPasswordAndScreenNameNotification(InputMeetingPasswordAndScreenNameNotification);
            meetingConfiguration.Add_CB_onWebinarNeedRegisterNotification(WebinarNeedRegisterNotification);

            var participantsController = meetingService.GetMeetingParticipantsController();
            participantsController.Add_CB_onHostChangeNotification(onHostChangeNotification);
            participantsController.Add_CB_onLowOrRaiseHandStatusChanged(onLowOrRaiseHandStatusChanged);
            participantsController.Add_CB_onUserJoin(onUserJoin);
            participantsController.Add_CB_onUserLeft(onUserLeft);
            participantsController.Add_CB_onUserNameChanged(onUserNameChanged);
        }

        void Connect()
        {
            RegisterCallBack();
            JoinParam param = new JoinParam();
            param.userType = SDKUserType.SDK_UT_WITHOUT_LOGIN;
            JoinParam4WithoutLogin join_api_param = new JoinParam4WithoutLogin();
            
            join_api_param.meetingNumber = CONST_MEETING_NUMBER;
            join_api_param.userName = CONST_USER_NAME;
            join_api_param.psw = CONST_MEETING_PASSWORD;
            param.withoutloginJoin = join_api_param;

            if (CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap().Join(param) != SDKError.SDKERR_SUCCESS)
                ShowMessageAndCloseApp("Can't connect!");
        }

        #endregion Connect

        #region AfterConnect

        IMeetingShareControllerDotNetWrap meetingShareController;
        ICustomizedShareRenderDotNetWrap icustomizedShareRenderer;
        IActiveVideoRenderElementDotNetWrap activeVideoRendererElement;
        ICustomizedVideoContainerDotNetWrap videoContainer;

        void DisplayVideo()
        {

            IntPtr handle = this.Handle;

            var ui = CZoomSDKeDotNetWrap.Instance.GetCustomizedUIMgrWrap();
            videoContainer = ui.CreateVideoContainer(handle, GetCurrentRect());

            var error = videoContainer.Show();
            if (error != SDKError.SDKERR_SUCCESS)
            {
                MessageBox.Show("Error at container.Show: " + error);
                return;
            }


            var videoRendererElement = videoContainer.CreateVideoElement(VideoRenderElementType.VideoRenderElement_ACTIVE);
            activeVideoRendererElement = videoRendererElement as IActiveVideoRenderElementDotNetWrap; 
            if (activeVideoRendererElement == null)
            {
                MessageBox.Show("No video!");
                return;
            }

            activeVideoRendererElement.Show();
            activeVideoRendererElement.Start();
            activeVideoRendererElement.EnableShowScreenNameOnVideo(true);

            if (error != SDKError.SDKERR_SUCCESS)
            {
                MessageBox.Show("activeVideo.Show() error: " + error);
                return;
            }
        }

        void StartDisplayShare(SharingStatus status, uint uid)
        {
            MessageBox.Show("SharingStatus" + status);
            if (status == SharingStatus.Sharing_Other_Share_Begin)
            {
                icustomizedShareRenderer.SetUserID(uid);
                //icustomized.SetViewMode(CustomizedViewShareMode.CSM_FULLFILL);
                icustomizedShareRenderer.Show();
            }
            else if (status == SharingStatus.Sharing_Other_Share_End)
            {
                icustomizedShareRenderer.Hide();
            }
        }

        void DisplayShareScreen()
        {
            IntPtr handle = this.Handle;
            IMeetingServiceDotNetWrap meetingService = CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap();
            meetingShareController = meetingService.GetMeetingShareController();

            meetingShareController.Add_CB_onSharingStatus(StartDisplayShare);//отписаться
            var ui = CZoomSDKeDotNetWrap.Instance.GetCustomizedUIMgrWrap();
            icustomizedShareRenderer = ui.CreateShareRender(handle, GetCurrentRect());

            icustomizedShareRenderer.Add_CB_onSharingContentStartRecving(() => MessageBox.Show("Add_CB_onSharingContentStartRecving"));
            icustomizedShareRenderer.Add_CB_onSharingSourceUserIDNotification((uint userId) => MessageBox.Show("uint userId : " + userId));

            var usersList = meetingShareController.GetViewableShareSourceList();

            if (usersList != null && usersList.Length > 0)
                StartDisplayShare(SharingStatus.Sharing_Other_Share_Begin, usersList[0]);
        }

        void ListenAudio()
        {
            IMeetingServiceDotNetWrap meetingService = CZoomSDKeDotNetWrap.Instance.GetMeetingServiceWrap();
            IMeetingAudioControllerDotNetWrap audioController = meetingService.GetMeetingAudioController();
            audioController.EnableMuteOnEntry(true);
            audioController.JoinVoip();
        }

        #endregion AfterConnect

        #region Misc
        void ShowMessageAndCloseApp(string message)
        {
            MessageBox.Show(message);
            this.Close();
        }

        RECT GetCurrentRect()
        {
            Rectangle clientRect = this.ClientRectangle;
            RECT rect = new RECT()
            {
                Left = clientRect.X,
                Top = clientRect.Y,
                Bottom = clientRect.Height,
                Right = clientRect.Width,
            };
            return rect;
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            UpdateContainers();
        }

        void UpdateContainers()
        {
            var rect = GetCurrentRect();
            icustomizedShareRenderer?.Resize(rect);
            videoContainer?.Resize(rect);
            activeVideoRendererElement?.SetPos(rect);
        }
        #endregion Misc
    }
}
