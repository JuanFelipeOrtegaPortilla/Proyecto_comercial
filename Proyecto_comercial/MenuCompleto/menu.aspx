<%@ Page Language="C#" AutoEventWireup="true" MasterPageFile="~/Sitio.Master" CodeBehind="menu.aspx.cs" Inherits="CooperativaCarchi.menu" %>
<%@ Register Src="~/PanelModulos.ascx" TagPrefix="Sistema" TagName="Modulos" %>
<asp:Content ID="Principal" ContentPlaceHolderID="Contenido" runat="server"><Sistema:Modulos ID="Catalogo" runat="server"/></asp:Content>