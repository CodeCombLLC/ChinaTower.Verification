function deleteDialog(url, id) {
    var html = '<div class="message-bg"></div><div class="message-outer"><div class="message-container">' +
        '<h3>提示信息</h3>' +
        '<p>点击删除按钮后，该记录将被永久删除，您确定要这样做吗？</p>' +
        '<div class="message-buttons"><a href="javascript:Delete(\'' + url + '\');deleteRow(\'' + id + '\')" class="btn btn-danger">删除</a> <a href="javascript:closeDialog()" class="btn">取消</a></div>' +
        '</div></div>';
    var dom = $(html);
    $('body').append(dom);
    $(".message-outer").css('margin-top', -($(".message-outer").outerHeight() / 2));
    setTimeout(function () { $(".message-bg").addClass('active'); $(".message-outer").addClass('active'); }, 10);
}

function closeDialog() {
    $('.message-bg').removeClass('active');
    $('.message-outer').removeClass('active');
    setTimeout(function () {
        $('.message-bg').remove();
        $('.message-outer').remove();
    }, 200);
}

function deleteRow(id)
{
    $('#' + id).remove();
    closeDialog();
}

var pos;

function serializeRules(rules)
{
    var obj = [];
    for (var i = 0; i < rules.length; i++) {
        var rule = $(rules[i]);
        var type = rule.attr('data-type');
        if (type) {
            switch(type)
            {
                case 'And':
                case 'Or':
                case 'Not':
                    obj.push({ Type: type, NestedRules: serializeRules(rule.children('ul').children('li')) });
                    break;
                case 'Empty':
                case 'NotEmpty':
                    obj.push({ Type: type, ArgumentIndex: rule.attr('data-argument-index') });
                    break;
                default:
                    obj.push({ Type: type, Expression: rule.attr('data-expression'), ArgumentIndex: rule.attr('data-argument-index') });
            }
        }
    }
    return obj;
}

function insertRule(p)
{
    pos = $(p).parent();
    console.log(pos);
    $('#modalInsertRule').modal('show');
}

function removeRule(p)
{
    $(p).parent('li').remove();
}

$(document).ready(function () {
    $('#lstRuleTypes').change(function () {
        switch ($('#lstRuleTypes').val()) {
            case 'And':
            case 'Or':
            case 'Not':
                $('#divIndexSelector').hide();
                $('#divExpression').hide();
                break;
            case 'Empty':
            case 'NotEmpty':
                $('#divIndexSelector').show();
                $('#divExpression').hide();
                break;
            default:
                $('#divIndexSelector').show();
                $('#divExpression').show();
                break;
        }
    });

    $('#btnInsertRule').click(function () {
        var str = '<li data-type="' + $('#lstRuleTypes').val() + '" data-expression="' + $('#txtExpression').val() + '" data-argument-index="' + $('#lstHeaders').val() + '">';
        switch ($('#lstRuleTypes').val())
        {
            case 'And':
                str += '<span>满足下面的全部子条件</span>';
                break;
            case 'Or':
                str += '<span>满足下面的任意一个子条件</span>';
                break;
            case 'Not':
                str += '<span>不可符合下列任意一个子条件</span>';
                break;
            case 'Equal':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' = ' + $('#txtExpression').val() + '</span>';
                break;
            case 'NotEqual':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ≠ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Gt':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ＞ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Gte':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ≥ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Lt':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ＜ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Lte':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' ≤ ' + $('#txtExpression').val() + '</span>';
                break;
            case 'Empty':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' 必须为空</span>';
                break;
            case 'NotEmpty':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' 不能为空</span>';
                break;
            case 'Regex':
                str += '<span>' + $('#lstHeaders').find('option:selected').text() + ' 满足正则表达式 ' + $('#txtExpression').val() + '</span>';
                break;
        }
        str += ' <a href="javascript:;" onclick="removeRule(this)">删除</a>';
        if ($('#lstRuleTypes').val() == 'And' || $('#lstRuleTypes').val() == 'Or' || $('#lstRuleTypes').val() == 'Not')
            str += '<ul><li class="li-insert"><a onclick="insertRule(this)" href="javascript:;">添加</a></li></ul>';
        str += '</li>';
        pos.before(str);
        $('#modalInsertRule').modal('hide');
    });

    $('#btnSaveRules').click(function () {
        var obj = serializeRules($('.rule-list').children('ul').children('li'));
        $('#ruleJson').val(JSON.stringify(obj));
        $('#frmSaveRule').submit();
    });

    $('#btnFormEditSubmit').click(function () {
        $.getJSON('/Station/Verify', $('#frmFormEdit').serialize(), function (data) {
            var fields = $('[name="fields"]');
            for (var i = 0; i < data.length; i++) {
                $(fields[data[i]]).addClass('has-error');
            }
            if (data.length > 0 && confirm('数据校验失败，是否继续提交？')) {
                $('#frmFormEdit').submit();
            }
            if (data.length == 0)
                $('#frmFormEdit').submit();
        });
    });

    $(document).click(function (e) {
        if ($(e.target).attr('name') == 'fields') {
            $(e.target).removeClass('has-error');
        }
    });
});

function editForm(id, type)
{
    $('#formType').val(type);
    $('#formId').val(id);
    $('#divEditForm').html('');
    $('#modalEditForm').modal('show');
    $.get('/Station/Fields/' + id, { }, function (data) {
        $('#divEditForm').html(data);
    });
}

function showVerifyDetails(id)
{
    $('#divVerifyLog').html('');
    $('#modalVerifyLog').modal('show');
    $.get('/Station/Log/' + id, { }, function (data) {
        $('#divVerifyLog').html(data);
    });
}

function showLogDetails(id) {
    $('#divLogs').html('');
    $('#modalLogs').modal('show');
    $.get('/Log/Detail/' + id, {}, function (data) {
        $('#divLogs').html(data);
    });
}