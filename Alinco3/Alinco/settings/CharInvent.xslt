<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
    xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:template match="/">
    <html>
      <head>
        <title>My Items</title>
        <link href="Dictionary.css" 
             rel="stylesheet" 
             type="text/css" />
      </head>
      <body>
        <div align="center">
          <h1>Inventory</h1>
        </div>

        <table border="1" 
            cellpadding="3" 
            cellspacing="0">
          <tr bgcolor="#9acd32">
          <th>Char</th>
	        <th>Category</th>
          <th>Item</th>
          <th>Description</th>
        </tr>
        <xsl:for-each select="//SDictionaryOfInt32InventoryItem">
        

          <xsl:for-each select="item/value/InventoryItem">
            <xsl:sort select="category"/>
            <xsl:sort select="epiccount" order="descending"/>
            <xsl:sort select="majorcount" order="descending"/>
            <xsl:sort select="itemset" order="descending"/>
	        <xsl:sort select="Name"/>
           <TR>           
            <TD><xsl:value-of select="charname"/></TD>
            <TD><xsl:value-of select="category"/></TD>
            <TD><xsl:value-of select="name"/></TD>
            <TD><xsl:value-of select="description"/></TD>
           </TR>

          </xsl:for-each>

        </xsl:for-each>

      </table>
    </body>
    </html>
  </xsl:template>
</xsl:stylesheet>