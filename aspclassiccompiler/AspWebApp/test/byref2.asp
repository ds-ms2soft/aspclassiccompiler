<%
    sub mysub(byval a)
        a = a + 1
        response.Write a
    end sub

    mysub 5

    Dim x
    x = 7
    response.write " (should be 6)<br/>"
    mysub x
    response.write " (should be 8)<br/>" & x & " (should be 7)"
%>