$(function () {
    //获取输入参数
    function getInput() {
        var input = {
            userName: $.trim($("#userName").val()),
            password: $.trim($("#password").val()),
            rememberLogin: !!$("#rememberLogin:checked").val(),
            returnUrl: $("#returnUrl").val(),
            __RequestVerificationToken: $("input[name='__RequestVerificationToken']:first").val()
        }
        return input;
    }
    //验证登录信息
    function validate() {
        var $userName = $("#userName");
        if ($.trim($userName.val()) === '') {
            $userName.focus();
            $("#lblUserName").show();
            return false;
        }

        var $password = $("#password");
        if ($.trim($password.val()) === '') {
            $password.focus();
            $("#lblPassword").show();
            return false;
        }
        return true;
    }
    //用户名检查
    $("#userName").blur(function () {
        if ($.trim($(this).val()) === '') {
            $("#lblUserName").show();
        } else {
            $("#lblUserName").hide();
        }
    });
    //密码检查
    $("#password").blur(function () {
        if ($.trim($(this).val()) === '') {
            $("#lblPassword").show();
        } else {
            $("#lblPassword").hide();
        }
    });

    var width = $('.form-group:first').width() + 'px';

    // 滑块验证
    var slideVerify = $('#content').slideVerify({
        baseUrl: 'http://localhost:8000',  //服务器请求地址, 默认地址为安吉服务器;
        containerId: '#btnLogin',//popup模式 必填 被点击之后出现行为验证码的元素id
        mode: 'embed',     //展示模式 embed popup
        imgSize: {       //图片的大小对象,有默认值{ width: '310px',height: '155px'},可省略
            width: width,
            height: '155px',
        },
        barSize: {          //下方滑块的大小对象,有默认值{ width: '310px',height: '40px'},可省略
            width: width,
            height: '40px',
        },
        beforeCheck: function () {  //检验参数合法性的函数  mode ="pop"有效
            var isValid = validate();
            if (!isValid) {
                return false;
            }

            return isValid
        },
        ready: function () { },  //加载完毕的回调
        success: function (params) { //成功的回调
            // params为返回的二次验证参数 需要在接下来的实现逻辑回传服务器
            var $me = $("#btnLogin");
            $me.prop('disabled', true).addClass('is-disabled').text('登录中...');
            var input = getInput();
            var timmerId = null;
            input.captcha = params
            $.post('/user/login', input, function (res) {
                if (!res) {
                    $me.prop('disabled', false).removeClass('is-disabled').text('重新登录');
                    return;
                }
                if (res.code === 1) {
                    var returnUrl = $.trim($("#returnUrl").val());
                    if (returnUrl) {
                        window.location.href = returnUrl;
                    }
                } else {
                    if (slideVerify) {
                        slideVerify.refresh();
                    }
                    
                    $me.prop('disabled', false).removeClass('is-disabled').text('重新登录');
                    var msg = res.msg;
                    if (res.data === 1) {
                        msg = '您的账号输入不正确，请重新输入';
                        $("#userName").focus();
                    } else if (res.data === 2) {
                        msg = '您的密码输入不正确，请重新输入';
                        $("#password").focus();
                    }
                    if (msg) {
                        $(".my-alert:first").show().text(msg);
                        if (timmerId) {
                            clearTimeout(timmerId);
                        }
                        timmerId = window.setTimeout(function () {
                            $(".my-alert:first").hide().text('');
                        }, 3000);
                    }
                }
            });
        },
        error: function () { }        //失败的回调
    });

    //登录
    $("#btnLogin").click(function () {
        return false;
    });

    var userDefaults = {
        plat: {
            userName: 'user',
            password: '111111'
        },
        tenant: {
            userName: '18988889999',
            password: '111111'
        }
    };
    $(".my-radio-group .my-radio-button__inner").click(function () {
        $(".my-radio-group .my-radio-button__inner.active").removeClass('active');
        $(this).addClass("active");
        var userType = $(this).data("value");
        var user = userDefaults[userType];
        if (user) {
            $("#userName").val(user.userName);
            $("#password").val(user.password);
        }
    });
});
