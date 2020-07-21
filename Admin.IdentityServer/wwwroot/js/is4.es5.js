//获取输入参数
"use strict";

function getInput() {
    var input = {
        userName: $.trim($("#userName").val()),
        password: $.trim($("#password").val()),
        rememberLogin: !!$("#rememberLogin:checked").val(),
        returnUrl: $("#returnUrl").val(),
        __RequestVerificationToken: $("input[name='__RequestVerificationToken']:first").val()
    };
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
//登录
$("#btnLogin").click(function () {
    var isValid = validate();
    if (!isValid) {
        return false;
    }

    var $me = $(this);
    $me.prop('disabled', true).addClass('is-disabled').text('登录中...');
    var input = getInput();
    var timmerId = null;
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
            $me.prop('disabled', false).removeClass('is-disabled').text('重新登录');
            var msg = '';
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

    return false;
});

