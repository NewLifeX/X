<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template match="/">
    <xsl:apply-templates select="TreeMenuRoot/Nodes"/>
  </xsl:template>
  <xsl:template match="TreeMenuRoot/Nodes">
    <xsl:apply-templates select="TreeMenuNode"/>
  </xsl:template>
  <xsl:template match="TreeMenuNode">
    <div onclick="clickOnEntity(this,event);" onselectstart="return false" ondragstart="return false" class="node_box">
      <xsl:attribute name="class">
        <xsl:if test="count(ancestor::*)=2">
          rootnode_box;
        </xsl:if>
        <xsl:if test="count(ancestor::*)>2">
          node_box;
        </xsl:if>
      </xsl:attribute>
      <xsl:attribute name="image">
        <xsl:value-of select="Image"/>
      </xsl:attribute>
      <xsl:attribute name="imageOpen">
        <xsl:value-of select="ImageOpen"/>
      </xsl:attribute>
      <xsl:attribute name="open">false</xsl:attribute>
      <xsl:attribute name="id">
        <xsl:value-of select="@ID"/>
      </xsl:attribute>
      <xsl:attribute name="open">false</xsl:attribute>
      <xsl:attribute name="url">
        <xsl:value-of select="Url"/>
      </xsl:attribute>
      <xsl:attribute name="STYLE">
        padding-left: 20px;
        <xsl:if test="count(ancestor::*)>2">
          display: none;
        </xsl:if>
      </xsl:attribute>
      <table border="0" cellspacing="0" cellpadding="0" style="cursor: pointer;">
        <tr>
          <xsl:attribute name="onclick">
            <xsl:text >jagascript:onClickMenu("</xsl:text>
              <xsl:value-of select="Url"/>
            <xsl:text >")</xsl:text>
          </xsl:attribute>
          <td class="node_image">
            <img border="0">
              <xsl:attribute name="id">
                <xsl:value-of select="@ID"/>
                <xsl:text >_image</xsl:text>
              </xsl:attribute>
              <xsl:attribute name="SRC">
                <xsl:value-of select="Image"/>
              </xsl:attribute>
            </img>
          </td>
          <td nowrap="true" class="node_title">
            <xsl:attribute name="STYLE">
              padding-left: 7px;
            </xsl:attribute>
            <xsl:value-of select="Title"/>
          </td>
        </tr>
      </table>
      <xsl:apply-templates select="Childs/TreeMenuNode"/>
    </div>
  </xsl:template>

</xsl:stylesheet>