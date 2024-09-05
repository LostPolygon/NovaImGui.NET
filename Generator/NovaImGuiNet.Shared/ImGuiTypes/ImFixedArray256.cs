using System.Collections;

namespace NovaImGuiNet;

public struct ImFixedArray256<T> : IList<T>, IEquatable<ImFixedArray256<T>> where T : unmanaged, IEquatable<T> {
    public T Value1;
    public T Value2;
    public T Value3;
    public T Value4;
    public T Value5;
    public T Value6;
    public T Value7;
    public T Value8;
    public T Value9;
    public T Value10;
    public T Value11;
    public T Value12;
    public T Value13;
    public T Value14;
    public T Value15;
    public T Value16;
    public T Value17;
    public T Value18;
    public T Value19;
    public T Value20;
    public T Value21;
    public T Value22;
    public T Value23;
    public T Value24;
    public T Value25;
    public T Value26;
    public T Value27;
    public T Value28;
    public T Value29;
    public T Value30;
    public T Value31;
    public T Value32;
    public T Value33;
    public T Value34;
    public T Value35;
    public T Value36;
    public T Value37;
    public T Value38;
    public T Value39;
    public T Value40;
    public T Value41;
    public T Value42;
    public T Value43;
    public T Value44;
    public T Value45;
    public T Value46;
    public T Value47;
    public T Value48;
    public T Value49;
    public T Value50;
    public T Value51;
    public T Value52;
    public T Value53;
    public T Value54;
    public T Value55;
    public T Value56;
    public T Value57;
    public T Value58;
    public T Value59;
    public T Value60;
    public T Value61;
    public T Value62;
    public T Value63;
    public T Value64;
    public T Value65;
    public T Value66;
    public T Value67;
    public T Value68;
    public T Value69;
    public T Value70;
    public T Value71;
    public T Value72;
    public T Value73;
    public T Value74;
    public T Value75;
    public T Value76;
    public T Value77;
    public T Value78;
    public T Value79;
    public T Value80;
    public T Value81;
    public T Value82;
    public T Value83;
    public T Value84;
    public T Value85;
    public T Value86;
    public T Value87;
    public T Value88;
    public T Value89;
    public T Value90;
    public T Value91;
    public T Value92;
    public T Value93;
    public T Value94;
    public T Value95;
    public T Value96;
    public T Value97;
    public T Value98;
    public T Value99;
    public T Value100;
    public T Value101;
    public T Value102;
    public T Value103;
    public T Value104;
    public T Value105;
    public T Value106;
    public T Value107;
    public T Value108;
    public T Value109;
    public T Value110;
    public T Value111;
    public T Value112;
    public T Value113;
    public T Value114;
    public T Value115;
    public T Value116;
    public T Value117;
    public T Value118;
    public T Value119;
    public T Value120;
    public T Value121;
    public T Value122;
    public T Value123;
    public T Value124;
    public T Value125;
    public T Value126;
    public T Value127;
    public T Value128;
    public T Value129;
    public T Value130;
    public T Value131;
    public T Value132;
    public T Value133;
    public T Value134;
    public T Value135;
    public T Value136;
    public T Value137;
    public T Value138;
    public T Value139;
    public T Value140;
    public T Value141;
    public T Value142;
    public T Value143;
    public T Value144;
    public T Value145;
    public T Value146;
    public T Value147;
    public T Value148;
    public T Value149;
    public T Value150;
    public T Value151;
    public T Value152;
    public T Value153;
    public T Value154;
    public T Value155;
    public T Value156;
    public T Value157;
    public T Value158;
    public T Value159;
    public T Value160;
    public T Value161;
    public T Value162;
    public T Value163;
    public T Value164;
    public T Value165;
    public T Value166;
    public T Value167;
    public T Value168;
    public T Value169;
    public T Value170;
    public T Value171;
    public T Value172;
    public T Value173;
    public T Value174;
    public T Value175;
    public T Value176;
    public T Value177;
    public T Value178;
    public T Value179;
    public T Value180;
    public T Value181;
    public T Value182;
    public T Value183;
    public T Value184;
    public T Value185;
    public T Value186;
    public T Value187;
    public T Value188;
    public T Value189;
    public T Value190;
    public T Value191;
    public T Value192;
    public T Value193;
    public T Value194;
    public T Value195;
    public T Value196;
    public T Value197;
    public T Value198;
    public T Value199;
    public T Value200;
    public T Value201;
    public T Value202;
    public T Value203;
    public T Value204;
    public T Value205;
    public T Value206;
    public T Value207;
    public T Value208;
    public T Value209;
    public T Value210;
    public T Value211;
    public T Value212;
    public T Value213;
    public T Value214;
    public T Value215;
    public T Value216;
    public T Value217;
    public T Value218;
    public T Value219;
    public T Value220;
    public T Value221;
    public T Value222;
    public T Value223;
    public T Value224;
    public T Value225;
    public T Value226;
    public T Value227;
    public T Value228;
    public T Value229;
    public T Value230;
    public T Value231;
    public T Value232;
    public T Value233;
    public T Value234;
    public T Value235;
    public T Value236;
    public T Value237;
    public T Value238;
    public T Value239;
    public T Value240;
    public T Value241;
    public T Value242;
    public T Value243;
    public T Value244;
    public T Value245;
    public T Value246;
    public T Value247;
    public T Value248;
    public T Value249;
    public T Value250;
    public T Value251;
    public T Value252;
    public T Value253;
    public T Value254;
    public T Value255;
    public T Value256;

    public T this[int index] {
        get {
            // Following trick can reduce the range check by one
            if ((uint) index >= 255)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray256 index must be between 0 and 255.");
            
            return Unsafe.Add(ref Value1, index);
        }
        set {
            // Following trick can reduce the range check by one
            if ((uint) index >= 255)
                throw new ArgumentOutOfRangeException(nameof(index), "ImFixedArray256 index must be between 0 and 255.");
            
            Unsafe.Add(ref Value1, index) = value;
        }
    }

    public ImFixedArray256(T value1, T value2, T value3, T value4, T value5, T value6, T value7, T value8, T value9, T value10, T value11, T value12, T value13, T value14, T value15, T value16, T value17, T value18, T value19, T value20, T value21, T value22, T value23, T value24, T value25, T value26, T value27, T value28, T value29, T value30, T value31, T value32, T value33, T value34, T value35, T value36, T value37, T value38, T value39, T value40, T value41, T value42, T value43, T value44, T value45, T value46, T value47, T value48, T value49, T value50, T value51, T value52, T value53, T value54, T value55, T value56, T value57, T value58, T value59, T value60, T value61, T value62, T value63, T value64, T value65, T value66, T value67, T value68, T value69, T value70, T value71, T value72, T value73, T value74, T value75, T value76, T value77, T value78, T value79, T value80, T value81, T value82, T value83, T value84, T value85, T value86, T value87, T value88, T value89, T value90, T value91, T value92, T value93, T value94, T value95, T value96, T value97, T value98, T value99, T value100, T value101, T value102, T value103, T value104, T value105, T value106, T value107, T value108, T value109, T value110, T value111, T value112, T value113, T value114, T value115, T value116, T value117, T value118, T value119, T value120, T value121, T value122, T value123, T value124, T value125, T value126, T value127, T value128, T value129, T value130, T value131, T value132, T value133, T value134, T value135, T value136, T value137, T value138, T value139, T value140, T value141, T value142, T value143, T value144, T value145, T value146, T value147, T value148, T value149, T value150, T value151, T value152, T value153, T value154, T value155, T value156, T value157, T value158, T value159, T value160, T value161, T value162, T value163, T value164, T value165, T value166, T value167, T value168, T value169, T value170, T value171, T value172, T value173, T value174, T value175, T value176, T value177, T value178, T value179, T value180, T value181, T value182, T value183, T value184, T value185, T value186, T value187, T value188, T value189, T value190, T value191, T value192, T value193, T value194, T value195, T value196, T value197, T value198, T value199, T value200, T value201, T value202, T value203, T value204, T value205, T value206, T value207, T value208, T value209, T value210, T value211, T value212, T value213, T value214, T value215, T value216, T value217, T value218, T value219, T value220, T value221, T value222, T value223, T value224, T value225, T value226, T value227, T value228, T value229, T value230, T value231, T value232, T value233, T value234, T value235, T value236, T value237, T value238, T value239, T value240, T value241, T value242, T value243, T value244, T value245, T value246, T value247, T value248, T value249, T value250, T value251, T value252, T value253, T value254, T value255, T value256) {
        Value1 = value1;
        Value2 = value2;
        Value3 = value3;
        Value4 = value4;
        Value5 = value5;
        Value6 = value6;
        Value7 = value7;
        Value8 = value8;
        Value9 = value9;
        Value10 = value10;
        Value11 = value11;
        Value12 = value12;
        Value13 = value13;
        Value14 = value14;
        Value15 = value15;
        Value16 = value16;
        Value17 = value17;
        Value18 = value18;
        Value19 = value19;
        Value20 = value20;
        Value21 = value21;
        Value22 = value22;
        Value23 = value23;
        Value24 = value24;
        Value25 = value25;
        Value26 = value26;
        Value27 = value27;
        Value28 = value28;
        Value29 = value29;
        Value30 = value30;
        Value31 = value31;
        Value32 = value32;
        Value33 = value33;
        Value34 = value34;
        Value35 = value35;
        Value36 = value36;
        Value37 = value37;
        Value38 = value38;
        Value39 = value39;
        Value40 = value40;
        Value41 = value41;
        Value42 = value42;
        Value43 = value43;
        Value44 = value44;
        Value45 = value45;
        Value46 = value46;
        Value47 = value47;
        Value48 = value48;
        Value49 = value49;
        Value50 = value50;
        Value51 = value51;
        Value52 = value52;
        Value53 = value53;
        Value54 = value54;
        Value55 = value55;
        Value56 = value56;
        Value57 = value57;
        Value58 = value58;
        Value59 = value59;
        Value60 = value60;
        Value61 = value61;
        Value62 = value62;
        Value63 = value63;
        Value64 = value64;
        Value65 = value65;
        Value66 = value66;
        Value67 = value67;
        Value68 = value68;
        Value69 = value69;
        Value70 = value70;
        Value71 = value71;
        Value72 = value72;
        Value73 = value73;
        Value74 = value74;
        Value75 = value75;
        Value76 = value76;
        Value77 = value77;
        Value78 = value78;
        Value79 = value79;
        Value80 = value80;
        Value81 = value81;
        Value82 = value82;
        Value83 = value83;
        Value84 = value84;
        Value85 = value85;
        Value86 = value86;
        Value87 = value87;
        Value88 = value88;
        Value89 = value89;
        Value90 = value90;
        Value91 = value91;
        Value92 = value92;
        Value93 = value93;
        Value94 = value94;
        Value95 = value95;
        Value96 = value96;
        Value97 = value97;
        Value98 = value98;
        Value99 = value99;
        Value100 = value100;
        Value101 = value101;
        Value102 = value102;
        Value103 = value103;
        Value104 = value104;
        Value105 = value105;
        Value106 = value106;
        Value107 = value107;
        Value108 = value108;
        Value109 = value109;
        Value110 = value110;
        Value111 = value111;
        Value112 = value112;
        Value113 = value113;
        Value114 = value114;
        Value115 = value115;
        Value116 = value116;
        Value117 = value117;
        Value118 = value118;
        Value119 = value119;
        Value120 = value120;
        Value121 = value121;
        Value122 = value122;
        Value123 = value123;
        Value124 = value124;
        Value125 = value125;
        Value126 = value126;
        Value127 = value127;
        Value128 = value128;
        Value129 = value129;
        Value130 = value130;
        Value131 = value131;
        Value132 = value132;
        Value133 = value133;
        Value134 = value134;
        Value135 = value135;
        Value136 = value136;
        Value137 = value137;
        Value138 = value138;
        Value139 = value139;
        Value140 = value140;
        Value141 = value141;
        Value142 = value142;
        Value143 = value143;
        Value144 = value144;
        Value145 = value145;
        Value146 = value146;
        Value147 = value147;
        Value148 = value148;
        Value149 = value149;
        Value150 = value150;
        Value151 = value151;
        Value152 = value152;
        Value153 = value153;
        Value154 = value154;
        Value155 = value155;
        Value156 = value156;
        Value157 = value157;
        Value158 = value158;
        Value159 = value159;
        Value160 = value160;
        Value161 = value161;
        Value162 = value162;
        Value163 = value163;
        Value164 = value164;
        Value165 = value165;
        Value166 = value166;
        Value167 = value167;
        Value168 = value168;
        Value169 = value169;
        Value170 = value170;
        Value171 = value171;
        Value172 = value172;
        Value173 = value173;
        Value174 = value174;
        Value175 = value175;
        Value176 = value176;
        Value177 = value177;
        Value178 = value178;
        Value179 = value179;
        Value180 = value180;
        Value181 = value181;
        Value182 = value182;
        Value183 = value183;
        Value184 = value184;
        Value185 = value185;
        Value186 = value186;
        Value187 = value187;
        Value188 = value188;
        Value189 = value189;
        Value190 = value190;
        Value191 = value191;
        Value192 = value192;
        Value193 = value193;
        Value194 = value194;
        Value195 = value195;
        Value196 = value196;
        Value197 = value197;
        Value198 = value198;
        Value199 = value199;
        Value200 = value200;
        Value201 = value201;
        Value202 = value202;
        Value203 = value203;
        Value204 = value204;
        Value205 = value205;
        Value206 = value206;
        Value207 = value207;
        Value208 = value208;
        Value209 = value209;
        Value210 = value210;
        Value211 = value211;
        Value212 = value212;
        Value213 = value213;
        Value214 = value214;
        Value215 = value215;
        Value216 = value216;
        Value217 = value217;
        Value218 = value218;
        Value219 = value219;
        Value220 = value220;
        Value221 = value221;
        Value222 = value222;
        Value223 = value223;
        Value224 = value224;
        Value225 = value225;
        Value226 = value226;
        Value227 = value227;
        Value228 = value228;
        Value229 = value229;
        Value230 = value230;
        Value231 = value231;
        Value232 = value232;
        Value233 = value233;
        Value234 = value234;
        Value235 = value235;
        Value236 = value236;
        Value237 = value237;
        Value238 = value238;
        Value239 = value239;
        Value240 = value240;
        Value241 = value241;
        Value242 = value242;
        Value243 = value243;
        Value244 = value244;
        Value245 = value245;
        Value246 = value246;
        Value247 = value247;
        Value248 = value248;
        Value249 = value249;
        Value250 = value250;
        Value251 = value251;
        Value252 = value252;
        Value253 = value253;
        Value254 = value254;
        Value255 = value255;
        Value256 = value256;
    }

    public unsafe Span<T> AsSpan() {
        fixed (T* ptr = &Value1) {
            return new Span<T>(ptr, 256);
        }
    }

    public bool Equals(ImFixedArray256<T> other) {
        return
            Value1.Equals(other.Value1) &&
            Value2.Equals(other.Value2) &&
            Value3.Equals(other.Value3) &&
            Value4.Equals(other.Value4) &&
            Value5.Equals(other.Value5) &&
            Value6.Equals(other.Value6) &&
            Value7.Equals(other.Value7) &&
            Value8.Equals(other.Value8) &&
            Value9.Equals(other.Value9) &&
            Value10.Equals(other.Value10) &&
            Value11.Equals(other.Value11) &&
            Value12.Equals(other.Value12) &&
            Value13.Equals(other.Value13) &&
            Value14.Equals(other.Value14) &&
            Value15.Equals(other.Value15) &&
            Value16.Equals(other.Value16) &&
            Value17.Equals(other.Value17) &&
            Value18.Equals(other.Value18) &&
            Value19.Equals(other.Value19) &&
            Value20.Equals(other.Value20) &&
            Value21.Equals(other.Value21) &&
            Value22.Equals(other.Value22) &&
            Value23.Equals(other.Value23) &&
            Value24.Equals(other.Value24) &&
            Value25.Equals(other.Value25) &&
            Value26.Equals(other.Value26) &&
            Value27.Equals(other.Value27) &&
            Value28.Equals(other.Value28) &&
            Value29.Equals(other.Value29) &&
            Value30.Equals(other.Value30) &&
            Value31.Equals(other.Value31) &&
            Value32.Equals(other.Value32) &&
            Value33.Equals(other.Value33) &&
            Value34.Equals(other.Value34) &&
            Value35.Equals(other.Value35) &&
            Value36.Equals(other.Value36) &&
            Value37.Equals(other.Value37) &&
            Value38.Equals(other.Value38) &&
            Value39.Equals(other.Value39) &&
            Value40.Equals(other.Value40) &&
            Value41.Equals(other.Value41) &&
            Value42.Equals(other.Value42) &&
            Value43.Equals(other.Value43) &&
            Value44.Equals(other.Value44) &&
            Value45.Equals(other.Value45) &&
            Value46.Equals(other.Value46) &&
            Value47.Equals(other.Value47) &&
            Value48.Equals(other.Value48) &&
            Value49.Equals(other.Value49) &&
            Value50.Equals(other.Value50) &&
            Value51.Equals(other.Value51) &&
            Value52.Equals(other.Value52) &&
            Value53.Equals(other.Value53) &&
            Value54.Equals(other.Value54) &&
            Value55.Equals(other.Value55) &&
            Value56.Equals(other.Value56) &&
            Value57.Equals(other.Value57) &&
            Value58.Equals(other.Value58) &&
            Value59.Equals(other.Value59) &&
            Value60.Equals(other.Value60) &&
            Value61.Equals(other.Value61) &&
            Value62.Equals(other.Value62) &&
            Value63.Equals(other.Value63) &&
            Value64.Equals(other.Value64) &&
            Value65.Equals(other.Value65) &&
            Value66.Equals(other.Value66) &&
            Value67.Equals(other.Value67) &&
            Value68.Equals(other.Value68) &&
            Value69.Equals(other.Value69) &&
            Value70.Equals(other.Value70) &&
            Value71.Equals(other.Value71) &&
            Value72.Equals(other.Value72) &&
            Value73.Equals(other.Value73) &&
            Value74.Equals(other.Value74) &&
            Value75.Equals(other.Value75) &&
            Value76.Equals(other.Value76) &&
            Value77.Equals(other.Value77) &&
            Value78.Equals(other.Value78) &&
            Value79.Equals(other.Value79) &&
            Value80.Equals(other.Value80) &&
            Value81.Equals(other.Value81) &&
            Value82.Equals(other.Value82) &&
            Value83.Equals(other.Value83) &&
            Value84.Equals(other.Value84) &&
            Value85.Equals(other.Value85) &&
            Value86.Equals(other.Value86) &&
            Value87.Equals(other.Value87) &&
            Value88.Equals(other.Value88) &&
            Value89.Equals(other.Value89) &&
            Value90.Equals(other.Value90) &&
            Value91.Equals(other.Value91) &&
            Value92.Equals(other.Value92) &&
            Value93.Equals(other.Value93) &&
            Value94.Equals(other.Value94) &&
            Value95.Equals(other.Value95) &&
            Value96.Equals(other.Value96) &&
            Value97.Equals(other.Value97) &&
            Value98.Equals(other.Value98) &&
            Value99.Equals(other.Value99) &&
            Value100.Equals(other.Value100) &&
            Value101.Equals(other.Value101) &&
            Value102.Equals(other.Value102) &&
            Value103.Equals(other.Value103) &&
            Value104.Equals(other.Value104) &&
            Value105.Equals(other.Value105) &&
            Value106.Equals(other.Value106) &&
            Value107.Equals(other.Value107) &&
            Value108.Equals(other.Value108) &&
            Value109.Equals(other.Value109) &&
            Value110.Equals(other.Value110) &&
            Value111.Equals(other.Value111) &&
            Value112.Equals(other.Value112) &&
            Value113.Equals(other.Value113) &&
            Value114.Equals(other.Value114) &&
            Value115.Equals(other.Value115) &&
            Value116.Equals(other.Value116) &&
            Value117.Equals(other.Value117) &&
            Value118.Equals(other.Value118) &&
            Value119.Equals(other.Value119) &&
            Value120.Equals(other.Value120) &&
            Value121.Equals(other.Value121) &&
            Value122.Equals(other.Value122) &&
            Value123.Equals(other.Value123) &&
            Value124.Equals(other.Value124) &&
            Value125.Equals(other.Value125) &&
            Value126.Equals(other.Value126) &&
            Value127.Equals(other.Value127) &&
            Value128.Equals(other.Value128) &&
            Value129.Equals(other.Value129) &&
            Value130.Equals(other.Value130) &&
            Value131.Equals(other.Value131) &&
            Value132.Equals(other.Value132) &&
            Value133.Equals(other.Value133) &&
            Value134.Equals(other.Value134) &&
            Value135.Equals(other.Value135) &&
            Value136.Equals(other.Value136) &&
            Value137.Equals(other.Value137) &&
            Value138.Equals(other.Value138) &&
            Value139.Equals(other.Value139) &&
            Value140.Equals(other.Value140) &&
            Value141.Equals(other.Value141) &&
            Value142.Equals(other.Value142) &&
            Value143.Equals(other.Value143) &&
            Value144.Equals(other.Value144) &&
            Value145.Equals(other.Value145) &&
            Value146.Equals(other.Value146) &&
            Value147.Equals(other.Value147) &&
            Value148.Equals(other.Value148) &&
            Value149.Equals(other.Value149) &&
            Value150.Equals(other.Value150) &&
            Value151.Equals(other.Value151) &&
            Value152.Equals(other.Value152) &&
            Value153.Equals(other.Value153) &&
            Value154.Equals(other.Value154) &&
            Value155.Equals(other.Value155) &&
            Value156.Equals(other.Value156) &&
            Value157.Equals(other.Value157) &&
            Value158.Equals(other.Value158) &&
            Value159.Equals(other.Value159) &&
            Value160.Equals(other.Value160) &&
            Value161.Equals(other.Value161) &&
            Value162.Equals(other.Value162) &&
            Value163.Equals(other.Value163) &&
            Value164.Equals(other.Value164) &&
            Value165.Equals(other.Value165) &&
            Value166.Equals(other.Value166) &&
            Value167.Equals(other.Value167) &&
            Value168.Equals(other.Value168) &&
            Value169.Equals(other.Value169) &&
            Value170.Equals(other.Value170) &&
            Value171.Equals(other.Value171) &&
            Value172.Equals(other.Value172) &&
            Value173.Equals(other.Value173) &&
            Value174.Equals(other.Value174) &&
            Value175.Equals(other.Value175) &&
            Value176.Equals(other.Value176) &&
            Value177.Equals(other.Value177) &&
            Value178.Equals(other.Value178) &&
            Value179.Equals(other.Value179) &&
            Value180.Equals(other.Value180) &&
            Value181.Equals(other.Value181) &&
            Value182.Equals(other.Value182) &&
            Value183.Equals(other.Value183) &&
            Value184.Equals(other.Value184) &&
            Value185.Equals(other.Value185) &&
            Value186.Equals(other.Value186) &&
            Value187.Equals(other.Value187) &&
            Value188.Equals(other.Value188) &&
            Value189.Equals(other.Value189) &&
            Value190.Equals(other.Value190) &&
            Value191.Equals(other.Value191) &&
            Value192.Equals(other.Value192) &&
            Value193.Equals(other.Value193) &&
            Value194.Equals(other.Value194) &&
            Value195.Equals(other.Value195) &&
            Value196.Equals(other.Value196) &&
            Value197.Equals(other.Value197) &&
            Value198.Equals(other.Value198) &&
            Value199.Equals(other.Value199) &&
            Value200.Equals(other.Value200) &&
            Value201.Equals(other.Value201) &&
            Value202.Equals(other.Value202) &&
            Value203.Equals(other.Value203) &&
            Value204.Equals(other.Value204) &&
            Value205.Equals(other.Value205) &&
            Value206.Equals(other.Value206) &&
            Value207.Equals(other.Value207) &&
            Value208.Equals(other.Value208) &&
            Value209.Equals(other.Value209) &&
            Value210.Equals(other.Value210) &&
            Value211.Equals(other.Value211) &&
            Value212.Equals(other.Value212) &&
            Value213.Equals(other.Value213) &&
            Value214.Equals(other.Value214) &&
            Value215.Equals(other.Value215) &&
            Value216.Equals(other.Value216) &&
            Value217.Equals(other.Value217) &&
            Value218.Equals(other.Value218) &&
            Value219.Equals(other.Value219) &&
            Value220.Equals(other.Value220) &&
            Value221.Equals(other.Value221) &&
            Value222.Equals(other.Value222) &&
            Value223.Equals(other.Value223) &&
            Value224.Equals(other.Value224) &&
            Value225.Equals(other.Value225) &&
            Value226.Equals(other.Value226) &&
            Value227.Equals(other.Value227) &&
            Value228.Equals(other.Value228) &&
            Value229.Equals(other.Value229) &&
            Value230.Equals(other.Value230) &&
            Value231.Equals(other.Value231) &&
            Value232.Equals(other.Value232) &&
            Value233.Equals(other.Value233) &&
            Value234.Equals(other.Value234) &&
            Value235.Equals(other.Value235) &&
            Value236.Equals(other.Value236) &&
            Value237.Equals(other.Value237) &&
            Value238.Equals(other.Value238) &&
            Value239.Equals(other.Value239) &&
            Value240.Equals(other.Value240) &&
            Value241.Equals(other.Value241) &&
            Value242.Equals(other.Value242) &&
            Value243.Equals(other.Value243) &&
            Value244.Equals(other.Value244) &&
            Value245.Equals(other.Value245) &&
            Value246.Equals(other.Value246) &&
            Value247.Equals(other.Value247) &&
            Value248.Equals(other.Value248) &&
            Value249.Equals(other.Value249) &&
            Value250.Equals(other.Value250) &&
            Value251.Equals(other.Value251) &&
            Value252.Equals(other.Value252) &&
            Value253.Equals(other.Value253) &&
            Value254.Equals(other.Value254) &&
            Value255.Equals(other.Value255) &&
            Value256.Equals(other.Value256);
    }

    public override bool Equals(object? obj) {
        return obj is ImFixedArray256<T> other && Equals(other);
    }

    public override int GetHashCode() {
        unchecked {
            int hashCode = Value1.GetHashCode();
            hashCode = (hashCode * 397) ^ Value2.GetHashCode();
            hashCode = (hashCode * 397) ^ Value3.GetHashCode();
            hashCode = (hashCode * 397) ^ Value4.GetHashCode();
            hashCode = (hashCode * 397) ^ Value5.GetHashCode();
            hashCode = (hashCode * 397) ^ Value6.GetHashCode();
            hashCode = (hashCode * 397) ^ Value7.GetHashCode();
            hashCode = (hashCode * 397) ^ Value8.GetHashCode();
            hashCode = (hashCode * 397) ^ Value9.GetHashCode();
            hashCode = (hashCode * 397) ^ Value10.GetHashCode();
            hashCode = (hashCode * 397) ^ Value11.GetHashCode();
            hashCode = (hashCode * 397) ^ Value12.GetHashCode();
            hashCode = (hashCode * 397) ^ Value13.GetHashCode();
            hashCode = (hashCode * 397) ^ Value14.GetHashCode();
            hashCode = (hashCode * 397) ^ Value15.GetHashCode();
            hashCode = (hashCode * 397) ^ Value16.GetHashCode();
            hashCode = (hashCode * 397) ^ Value17.GetHashCode();
            hashCode = (hashCode * 397) ^ Value18.GetHashCode();
            hashCode = (hashCode * 397) ^ Value19.GetHashCode();
            hashCode = (hashCode * 397) ^ Value20.GetHashCode();
            hashCode = (hashCode * 397) ^ Value21.GetHashCode();
            hashCode = (hashCode * 397) ^ Value22.GetHashCode();
            hashCode = (hashCode * 397) ^ Value23.GetHashCode();
            hashCode = (hashCode * 397) ^ Value24.GetHashCode();
            hashCode = (hashCode * 397) ^ Value25.GetHashCode();
            hashCode = (hashCode * 397) ^ Value26.GetHashCode();
            hashCode = (hashCode * 397) ^ Value27.GetHashCode();
            hashCode = (hashCode * 397) ^ Value28.GetHashCode();
            hashCode = (hashCode * 397) ^ Value29.GetHashCode();
            hashCode = (hashCode * 397) ^ Value30.GetHashCode();
            hashCode = (hashCode * 397) ^ Value31.GetHashCode();
            hashCode = (hashCode * 397) ^ Value32.GetHashCode();
            hashCode = (hashCode * 397) ^ Value33.GetHashCode();
            hashCode = (hashCode * 397) ^ Value34.GetHashCode();
            hashCode = (hashCode * 397) ^ Value35.GetHashCode();
            hashCode = (hashCode * 397) ^ Value36.GetHashCode();
            hashCode = (hashCode * 397) ^ Value37.GetHashCode();
            hashCode = (hashCode * 397) ^ Value38.GetHashCode();
            hashCode = (hashCode * 397) ^ Value39.GetHashCode();
            hashCode = (hashCode * 397) ^ Value40.GetHashCode();
            hashCode = (hashCode * 397) ^ Value41.GetHashCode();
            hashCode = (hashCode * 397) ^ Value42.GetHashCode();
            hashCode = (hashCode * 397) ^ Value43.GetHashCode();
            hashCode = (hashCode * 397) ^ Value44.GetHashCode();
            hashCode = (hashCode * 397) ^ Value45.GetHashCode();
            hashCode = (hashCode * 397) ^ Value46.GetHashCode();
            hashCode = (hashCode * 397) ^ Value47.GetHashCode();
            hashCode = (hashCode * 397) ^ Value48.GetHashCode();
            hashCode = (hashCode * 397) ^ Value49.GetHashCode();
            hashCode = (hashCode * 397) ^ Value50.GetHashCode();
            hashCode = (hashCode * 397) ^ Value51.GetHashCode();
            hashCode = (hashCode * 397) ^ Value52.GetHashCode();
            hashCode = (hashCode * 397) ^ Value53.GetHashCode();
            hashCode = (hashCode * 397) ^ Value54.GetHashCode();
            hashCode = (hashCode * 397) ^ Value55.GetHashCode();
            hashCode = (hashCode * 397) ^ Value56.GetHashCode();
            hashCode = (hashCode * 397) ^ Value57.GetHashCode();
            hashCode = (hashCode * 397) ^ Value58.GetHashCode();
            hashCode = (hashCode * 397) ^ Value59.GetHashCode();
            hashCode = (hashCode * 397) ^ Value60.GetHashCode();
            hashCode = (hashCode * 397) ^ Value61.GetHashCode();
            hashCode = (hashCode * 397) ^ Value62.GetHashCode();
            hashCode = (hashCode * 397) ^ Value63.GetHashCode();
            hashCode = (hashCode * 397) ^ Value64.GetHashCode();
            hashCode = (hashCode * 397) ^ Value65.GetHashCode();
            hashCode = (hashCode * 397) ^ Value66.GetHashCode();
            hashCode = (hashCode * 397) ^ Value67.GetHashCode();
            hashCode = (hashCode * 397) ^ Value68.GetHashCode();
            hashCode = (hashCode * 397) ^ Value69.GetHashCode();
            hashCode = (hashCode * 397) ^ Value70.GetHashCode();
            hashCode = (hashCode * 397) ^ Value71.GetHashCode();
            hashCode = (hashCode * 397) ^ Value72.GetHashCode();
            hashCode = (hashCode * 397) ^ Value73.GetHashCode();
            hashCode = (hashCode * 397) ^ Value74.GetHashCode();
            hashCode = (hashCode * 397) ^ Value75.GetHashCode();
            hashCode = (hashCode * 397) ^ Value76.GetHashCode();
            hashCode = (hashCode * 397) ^ Value77.GetHashCode();
            hashCode = (hashCode * 397) ^ Value78.GetHashCode();
            hashCode = (hashCode * 397) ^ Value79.GetHashCode();
            hashCode = (hashCode * 397) ^ Value80.GetHashCode();
            hashCode = (hashCode * 397) ^ Value81.GetHashCode();
            hashCode = (hashCode * 397) ^ Value82.GetHashCode();
            hashCode = (hashCode * 397) ^ Value83.GetHashCode();
            hashCode = (hashCode * 397) ^ Value84.GetHashCode();
            hashCode = (hashCode * 397) ^ Value85.GetHashCode();
            hashCode = (hashCode * 397) ^ Value86.GetHashCode();
            hashCode = (hashCode * 397) ^ Value87.GetHashCode();
            hashCode = (hashCode * 397) ^ Value88.GetHashCode();
            hashCode = (hashCode * 397) ^ Value89.GetHashCode();
            hashCode = (hashCode * 397) ^ Value90.GetHashCode();
            hashCode = (hashCode * 397) ^ Value91.GetHashCode();
            hashCode = (hashCode * 397) ^ Value92.GetHashCode();
            hashCode = (hashCode * 397) ^ Value93.GetHashCode();
            hashCode = (hashCode * 397) ^ Value94.GetHashCode();
            hashCode = (hashCode * 397) ^ Value95.GetHashCode();
            hashCode = (hashCode * 397) ^ Value96.GetHashCode();
            hashCode = (hashCode * 397) ^ Value97.GetHashCode();
            hashCode = (hashCode * 397) ^ Value98.GetHashCode();
            hashCode = (hashCode * 397) ^ Value99.GetHashCode();
            hashCode = (hashCode * 397) ^ Value100.GetHashCode();
            hashCode = (hashCode * 397) ^ Value101.GetHashCode();
            hashCode = (hashCode * 397) ^ Value102.GetHashCode();
            hashCode = (hashCode * 397) ^ Value103.GetHashCode();
            hashCode = (hashCode * 397) ^ Value104.GetHashCode();
            hashCode = (hashCode * 397) ^ Value105.GetHashCode();
            hashCode = (hashCode * 397) ^ Value106.GetHashCode();
            hashCode = (hashCode * 397) ^ Value107.GetHashCode();
            hashCode = (hashCode * 397) ^ Value108.GetHashCode();
            hashCode = (hashCode * 397) ^ Value109.GetHashCode();
            hashCode = (hashCode * 397) ^ Value110.GetHashCode();
            hashCode = (hashCode * 397) ^ Value111.GetHashCode();
            hashCode = (hashCode * 397) ^ Value112.GetHashCode();
            hashCode = (hashCode * 397) ^ Value113.GetHashCode();
            hashCode = (hashCode * 397) ^ Value114.GetHashCode();
            hashCode = (hashCode * 397) ^ Value115.GetHashCode();
            hashCode = (hashCode * 397) ^ Value116.GetHashCode();
            hashCode = (hashCode * 397) ^ Value117.GetHashCode();
            hashCode = (hashCode * 397) ^ Value118.GetHashCode();
            hashCode = (hashCode * 397) ^ Value119.GetHashCode();
            hashCode = (hashCode * 397) ^ Value120.GetHashCode();
            hashCode = (hashCode * 397) ^ Value121.GetHashCode();
            hashCode = (hashCode * 397) ^ Value122.GetHashCode();
            hashCode = (hashCode * 397) ^ Value123.GetHashCode();
            hashCode = (hashCode * 397) ^ Value124.GetHashCode();
            hashCode = (hashCode * 397) ^ Value125.GetHashCode();
            hashCode = (hashCode * 397) ^ Value126.GetHashCode();
            hashCode = (hashCode * 397) ^ Value127.GetHashCode();
            hashCode = (hashCode * 397) ^ Value128.GetHashCode();
            hashCode = (hashCode * 397) ^ Value129.GetHashCode();
            hashCode = (hashCode * 397) ^ Value130.GetHashCode();
            hashCode = (hashCode * 397) ^ Value131.GetHashCode();
            hashCode = (hashCode * 397) ^ Value132.GetHashCode();
            hashCode = (hashCode * 397) ^ Value133.GetHashCode();
            hashCode = (hashCode * 397) ^ Value134.GetHashCode();
            hashCode = (hashCode * 397) ^ Value135.GetHashCode();
            hashCode = (hashCode * 397) ^ Value136.GetHashCode();
            hashCode = (hashCode * 397) ^ Value137.GetHashCode();
            hashCode = (hashCode * 397) ^ Value138.GetHashCode();
            hashCode = (hashCode * 397) ^ Value139.GetHashCode();
            hashCode = (hashCode * 397) ^ Value140.GetHashCode();
            hashCode = (hashCode * 397) ^ Value141.GetHashCode();
            hashCode = (hashCode * 397) ^ Value142.GetHashCode();
            hashCode = (hashCode * 397) ^ Value143.GetHashCode();
            hashCode = (hashCode * 397) ^ Value144.GetHashCode();
            hashCode = (hashCode * 397) ^ Value145.GetHashCode();
            hashCode = (hashCode * 397) ^ Value146.GetHashCode();
            hashCode = (hashCode * 397) ^ Value147.GetHashCode();
            hashCode = (hashCode * 397) ^ Value148.GetHashCode();
            hashCode = (hashCode * 397) ^ Value149.GetHashCode();
            hashCode = (hashCode * 397) ^ Value150.GetHashCode();
            hashCode = (hashCode * 397) ^ Value151.GetHashCode();
            hashCode = (hashCode * 397) ^ Value152.GetHashCode();
            hashCode = (hashCode * 397) ^ Value153.GetHashCode();
            hashCode = (hashCode * 397) ^ Value154.GetHashCode();
            hashCode = (hashCode * 397) ^ Value155.GetHashCode();
            hashCode = (hashCode * 397) ^ Value156.GetHashCode();
            hashCode = (hashCode * 397) ^ Value157.GetHashCode();
            hashCode = (hashCode * 397) ^ Value158.GetHashCode();
            hashCode = (hashCode * 397) ^ Value159.GetHashCode();
            hashCode = (hashCode * 397) ^ Value160.GetHashCode();
            hashCode = (hashCode * 397) ^ Value161.GetHashCode();
            hashCode = (hashCode * 397) ^ Value162.GetHashCode();
            hashCode = (hashCode * 397) ^ Value163.GetHashCode();
            hashCode = (hashCode * 397) ^ Value164.GetHashCode();
            hashCode = (hashCode * 397) ^ Value165.GetHashCode();
            hashCode = (hashCode * 397) ^ Value166.GetHashCode();
            hashCode = (hashCode * 397) ^ Value167.GetHashCode();
            hashCode = (hashCode * 397) ^ Value168.GetHashCode();
            hashCode = (hashCode * 397) ^ Value169.GetHashCode();
            hashCode = (hashCode * 397) ^ Value170.GetHashCode();
            hashCode = (hashCode * 397) ^ Value171.GetHashCode();
            hashCode = (hashCode * 397) ^ Value172.GetHashCode();
            hashCode = (hashCode * 397) ^ Value173.GetHashCode();
            hashCode = (hashCode * 397) ^ Value174.GetHashCode();
            hashCode = (hashCode * 397) ^ Value175.GetHashCode();
            hashCode = (hashCode * 397) ^ Value176.GetHashCode();
            hashCode = (hashCode * 397) ^ Value177.GetHashCode();
            hashCode = (hashCode * 397) ^ Value178.GetHashCode();
            hashCode = (hashCode * 397) ^ Value179.GetHashCode();
            hashCode = (hashCode * 397) ^ Value180.GetHashCode();
            hashCode = (hashCode * 397) ^ Value181.GetHashCode();
            hashCode = (hashCode * 397) ^ Value182.GetHashCode();
            hashCode = (hashCode * 397) ^ Value183.GetHashCode();
            hashCode = (hashCode * 397) ^ Value184.GetHashCode();
            hashCode = (hashCode * 397) ^ Value185.GetHashCode();
            hashCode = (hashCode * 397) ^ Value186.GetHashCode();
            hashCode = (hashCode * 397) ^ Value187.GetHashCode();
            hashCode = (hashCode * 397) ^ Value188.GetHashCode();
            hashCode = (hashCode * 397) ^ Value189.GetHashCode();
            hashCode = (hashCode * 397) ^ Value190.GetHashCode();
            hashCode = (hashCode * 397) ^ Value191.GetHashCode();
            hashCode = (hashCode * 397) ^ Value192.GetHashCode();
            hashCode = (hashCode * 397) ^ Value193.GetHashCode();
            hashCode = (hashCode * 397) ^ Value194.GetHashCode();
            hashCode = (hashCode * 397) ^ Value195.GetHashCode();
            hashCode = (hashCode * 397) ^ Value196.GetHashCode();
            hashCode = (hashCode * 397) ^ Value197.GetHashCode();
            hashCode = (hashCode * 397) ^ Value198.GetHashCode();
            hashCode = (hashCode * 397) ^ Value199.GetHashCode();
            hashCode = (hashCode * 397) ^ Value200.GetHashCode();
            hashCode = (hashCode * 397) ^ Value201.GetHashCode();
            hashCode = (hashCode * 397) ^ Value202.GetHashCode();
            hashCode = (hashCode * 397) ^ Value203.GetHashCode();
            hashCode = (hashCode * 397) ^ Value204.GetHashCode();
            hashCode = (hashCode * 397) ^ Value205.GetHashCode();
            hashCode = (hashCode * 397) ^ Value206.GetHashCode();
            hashCode = (hashCode * 397) ^ Value207.GetHashCode();
            hashCode = (hashCode * 397) ^ Value208.GetHashCode();
            hashCode = (hashCode * 397) ^ Value209.GetHashCode();
            hashCode = (hashCode * 397) ^ Value210.GetHashCode();
            hashCode = (hashCode * 397) ^ Value211.GetHashCode();
            hashCode = (hashCode * 397) ^ Value212.GetHashCode();
            hashCode = (hashCode * 397) ^ Value213.GetHashCode();
            hashCode = (hashCode * 397) ^ Value214.GetHashCode();
            hashCode = (hashCode * 397) ^ Value215.GetHashCode();
            hashCode = (hashCode * 397) ^ Value216.GetHashCode();
            hashCode = (hashCode * 397) ^ Value217.GetHashCode();
            hashCode = (hashCode * 397) ^ Value218.GetHashCode();
            hashCode = (hashCode * 397) ^ Value219.GetHashCode();
            hashCode = (hashCode * 397) ^ Value220.GetHashCode();
            hashCode = (hashCode * 397) ^ Value221.GetHashCode();
            hashCode = (hashCode * 397) ^ Value222.GetHashCode();
            hashCode = (hashCode * 397) ^ Value223.GetHashCode();
            hashCode = (hashCode * 397) ^ Value224.GetHashCode();
            hashCode = (hashCode * 397) ^ Value225.GetHashCode();
            hashCode = (hashCode * 397) ^ Value226.GetHashCode();
            hashCode = (hashCode * 397) ^ Value227.GetHashCode();
            hashCode = (hashCode * 397) ^ Value228.GetHashCode();
            hashCode = (hashCode * 397) ^ Value229.GetHashCode();
            hashCode = (hashCode * 397) ^ Value230.GetHashCode();
            hashCode = (hashCode * 397) ^ Value231.GetHashCode();
            hashCode = (hashCode * 397) ^ Value232.GetHashCode();
            hashCode = (hashCode * 397) ^ Value233.GetHashCode();
            hashCode = (hashCode * 397) ^ Value234.GetHashCode();
            hashCode = (hashCode * 397) ^ Value235.GetHashCode();
            hashCode = (hashCode * 397) ^ Value236.GetHashCode();
            hashCode = (hashCode * 397) ^ Value237.GetHashCode();
            hashCode = (hashCode * 397) ^ Value238.GetHashCode();
            hashCode = (hashCode * 397) ^ Value239.GetHashCode();
            hashCode = (hashCode * 397) ^ Value240.GetHashCode();
            hashCode = (hashCode * 397) ^ Value241.GetHashCode();
            hashCode = (hashCode * 397) ^ Value242.GetHashCode();
            hashCode = (hashCode * 397) ^ Value243.GetHashCode();
            hashCode = (hashCode * 397) ^ Value244.GetHashCode();
            hashCode = (hashCode * 397) ^ Value245.GetHashCode();
            hashCode = (hashCode * 397) ^ Value246.GetHashCode();
            hashCode = (hashCode * 397) ^ Value247.GetHashCode();
            hashCode = (hashCode * 397) ^ Value248.GetHashCode();
            hashCode = (hashCode * 397) ^ Value249.GetHashCode();
            hashCode = (hashCode * 397) ^ Value250.GetHashCode();
            hashCode = (hashCode * 397) ^ Value251.GetHashCode();
            hashCode = (hashCode * 397) ^ Value252.GetHashCode();
            hashCode = (hashCode * 397) ^ Value253.GetHashCode();
            hashCode = (hashCode * 397) ^ Value254.GetHashCode();
            hashCode = (hashCode * 397) ^ Value255.GetHashCode();
            hashCode = (hashCode * 397) ^ Value256.GetHashCode();
            return hashCode;
        }
    }

    public override string ToString() {
        return
            $"{nameof(Value1)}: {Value1}, " +
            $"{nameof(Value2)}: {Value2}, " +
            $"{nameof(Value3)}: {Value3}, " +
            $"{nameof(Value4)}: {Value4}, " +
            $"{nameof(Value5)}: {Value5}, " +
            $"{nameof(Value6)}: {Value6}, " +
            $"{nameof(Value7)}: {Value7}, " +
            $"{nameof(Value8)}: {Value8}, " +
            $"{nameof(Value9)}: {Value9}, " +
            $"{nameof(Value10)}: {Value10}, " +
            $"{nameof(Value11)}: {Value11}, " +
            $"{nameof(Value12)}: {Value12}, " +
            $"{nameof(Value13)}: {Value13}, " +
            $"{nameof(Value14)}: {Value14}, " +
            $"{nameof(Value15)}: {Value15}, " +
            $"{nameof(Value16)}: {Value16}, " +
            $"{nameof(Value17)}: {Value17}, " +
            $"{nameof(Value18)}: {Value18}, " +
            $"{nameof(Value19)}: {Value19}, " +
            $"{nameof(Value20)}: {Value20}, " +
            $"{nameof(Value21)}: {Value21}, " +
            $"{nameof(Value22)}: {Value22}, " +
            $"{nameof(Value23)}: {Value23}, " +
            $"{nameof(Value24)}: {Value24}, " +
            $"{nameof(Value25)}: {Value25}, " +
            $"{nameof(Value26)}: {Value26}, " +
            $"{nameof(Value27)}: {Value27}, " +
            $"{nameof(Value28)}: {Value28}, " +
            $"{nameof(Value29)}: {Value29}, " +
            $"{nameof(Value30)}: {Value30}, " +
            $"{nameof(Value31)}: {Value31}, " +
            $"{nameof(Value32)}: {Value32}, " +
            $"{nameof(Value33)}: {Value33}, " +
            $"{nameof(Value34)}: {Value34}, " +
            $"{nameof(Value35)}: {Value35}, " +
            $"{nameof(Value36)}: {Value36}, " +
            $"{nameof(Value37)}: {Value37}, " +
            $"{nameof(Value38)}: {Value38}, " +
            $"{nameof(Value39)}: {Value39}, " +
            $"{nameof(Value40)}: {Value40}, " +
            $"{nameof(Value41)}: {Value41}, " +
            $"{nameof(Value42)}: {Value42}, " +
            $"{nameof(Value43)}: {Value43}, " +
            $"{nameof(Value44)}: {Value44}, " +
            $"{nameof(Value45)}: {Value45}, " +
            $"{nameof(Value46)}: {Value46}, " +
            $"{nameof(Value47)}: {Value47}, " +
            $"{nameof(Value48)}: {Value48}, " +
            $"{nameof(Value49)}: {Value49}, " +
            $"{nameof(Value50)}: {Value50}, " +
            $"{nameof(Value51)}: {Value51}, " +
            $"{nameof(Value52)}: {Value52}, " +
            $"{nameof(Value53)}: {Value53}, " +
            $"{nameof(Value54)}: {Value54}, " +
            $"{nameof(Value55)}: {Value55}, " +
            $"{nameof(Value56)}: {Value56}, " +
            $"{nameof(Value57)}: {Value57}, " +
            $"{nameof(Value58)}: {Value58}, " +
            $"{nameof(Value59)}: {Value59}, " +
            $"{nameof(Value60)}: {Value60}, " +
            $"{nameof(Value61)}: {Value61}, " +
            $"{nameof(Value62)}: {Value62}, " +
            $"{nameof(Value63)}: {Value63}, " +
            $"{nameof(Value64)}: {Value64}, " +
            $"{nameof(Value65)}: {Value65}, " +
            $"{nameof(Value66)}: {Value66}, " +
            $"{nameof(Value67)}: {Value67}, " +
            $"{nameof(Value68)}: {Value68}, " +
            $"{nameof(Value69)}: {Value69}, " +
            $"{nameof(Value70)}: {Value70}, " +
            $"{nameof(Value71)}: {Value71}, " +
            $"{nameof(Value72)}: {Value72}, " +
            $"{nameof(Value73)}: {Value73}, " +
            $"{nameof(Value74)}: {Value74}, " +
            $"{nameof(Value75)}: {Value75}, " +
            $"{nameof(Value76)}: {Value76}, " +
            $"{nameof(Value77)}: {Value77}, " +
            $"{nameof(Value78)}: {Value78}, " +
            $"{nameof(Value79)}: {Value79}, " +
            $"{nameof(Value80)}: {Value80}, " +
            $"{nameof(Value81)}: {Value81}, " +
            $"{nameof(Value82)}: {Value82}, " +
            $"{nameof(Value83)}: {Value83}, " +
            $"{nameof(Value84)}: {Value84}, " +
            $"{nameof(Value85)}: {Value85}, " +
            $"{nameof(Value86)}: {Value86}, " +
            $"{nameof(Value87)}: {Value87}, " +
            $"{nameof(Value88)}: {Value88}, " +
            $"{nameof(Value89)}: {Value89}, " +
            $"{nameof(Value90)}: {Value90}, " +
            $"{nameof(Value91)}: {Value91}, " +
            $"{nameof(Value92)}: {Value92}, " +
            $"{nameof(Value93)}: {Value93}, " +
            $"{nameof(Value94)}: {Value94}, " +
            $"{nameof(Value95)}: {Value95}, " +
            $"{nameof(Value96)}: {Value96}, " +
            $"{nameof(Value97)}: {Value97}, " +
            $"{nameof(Value98)}: {Value98}, " +
            $"{nameof(Value99)}: {Value99}, " +
            $"{nameof(Value100)}: {Value100}, " +
            $"{nameof(Value101)}: {Value101}, " +
            $"{nameof(Value102)}: {Value102}, " +
            $"{nameof(Value103)}: {Value103}, " +
            $"{nameof(Value104)}: {Value104}, " +
            $"{nameof(Value105)}: {Value105}, " +
            $"{nameof(Value106)}: {Value106}, " +
            $"{nameof(Value107)}: {Value107}, " +
            $"{nameof(Value108)}: {Value108}, " +
            $"{nameof(Value109)}: {Value109}, " +
            $"{nameof(Value110)}: {Value110}, " +
            $"{nameof(Value111)}: {Value111}, " +
            $"{nameof(Value112)}: {Value112}, " +
            $"{nameof(Value113)}: {Value113}, " +
            $"{nameof(Value114)}: {Value114}, " +
            $"{nameof(Value115)}: {Value115}, " +
            $"{nameof(Value116)}: {Value116}, " +
            $"{nameof(Value117)}: {Value117}, " +
            $"{nameof(Value118)}: {Value118}, " +
            $"{nameof(Value119)}: {Value119}, " +
            $"{nameof(Value120)}: {Value120}, " +
            $"{nameof(Value121)}: {Value121}, " +
            $"{nameof(Value122)}: {Value122}, " +
            $"{nameof(Value123)}: {Value123}, " +
            $"{nameof(Value124)}: {Value124}, " +
            $"{nameof(Value125)}: {Value125}, " +
            $"{nameof(Value126)}: {Value126}, " +
            $"{nameof(Value127)}: {Value127}, " +
            $"{nameof(Value128)}: {Value128}, " +
            $"{nameof(Value129)}: {Value129}, " +
            $"{nameof(Value130)}: {Value130}, " +
            $"{nameof(Value131)}: {Value131}, " +
            $"{nameof(Value132)}: {Value132}, " +
            $"{nameof(Value133)}: {Value133}, " +
            $"{nameof(Value134)}: {Value134}, " +
            $"{nameof(Value135)}: {Value135}, " +
            $"{nameof(Value136)}: {Value136}, " +
            $"{nameof(Value137)}: {Value137}, " +
            $"{nameof(Value138)}: {Value138}, " +
            $"{nameof(Value139)}: {Value139}, " +
            $"{nameof(Value140)}: {Value140}, " +
            $"{nameof(Value141)}: {Value141}, " +
            $"{nameof(Value142)}: {Value142}, " +
            $"{nameof(Value143)}: {Value143}, " +
            $"{nameof(Value144)}: {Value144}, " +
            $"{nameof(Value145)}: {Value145}, " +
            $"{nameof(Value146)}: {Value146}, " +
            $"{nameof(Value147)}: {Value147}, " +
            $"{nameof(Value148)}: {Value148}, " +
            $"{nameof(Value149)}: {Value149}, " +
            $"{nameof(Value150)}: {Value150}, " +
            $"{nameof(Value151)}: {Value151}, " +
            $"{nameof(Value152)}: {Value152}, " +
            $"{nameof(Value153)}: {Value153}, " +
            $"{nameof(Value154)}: {Value154}, " +
            $"{nameof(Value155)}: {Value155}, " +
            $"{nameof(Value156)}: {Value156}, " +
            $"{nameof(Value157)}: {Value157}, " +
            $"{nameof(Value158)}: {Value158}, " +
            $"{nameof(Value159)}: {Value159}, " +
            $"{nameof(Value160)}: {Value160}, " +
            $"{nameof(Value161)}: {Value161}, " +
            $"{nameof(Value162)}: {Value162}, " +
            $"{nameof(Value163)}: {Value163}, " +
            $"{nameof(Value164)}: {Value164}, " +
            $"{nameof(Value165)}: {Value165}, " +
            $"{nameof(Value166)}: {Value166}, " +
            $"{nameof(Value167)}: {Value167}, " +
            $"{nameof(Value168)}: {Value168}, " +
            $"{nameof(Value169)}: {Value169}, " +
            $"{nameof(Value170)}: {Value170}, " +
            $"{nameof(Value171)}: {Value171}, " +
            $"{nameof(Value172)}: {Value172}, " +
            $"{nameof(Value173)}: {Value173}, " +
            $"{nameof(Value174)}: {Value174}, " +
            $"{nameof(Value175)}: {Value175}, " +
            $"{nameof(Value176)}: {Value176}, " +
            $"{nameof(Value177)}: {Value177}, " +
            $"{nameof(Value178)}: {Value178}, " +
            $"{nameof(Value179)}: {Value179}, " +
            $"{nameof(Value180)}: {Value180}, " +
            $"{nameof(Value181)}: {Value181}, " +
            $"{nameof(Value182)}: {Value182}, " +
            $"{nameof(Value183)}: {Value183}, " +
            $"{nameof(Value184)}: {Value184}, " +
            $"{nameof(Value185)}: {Value185}, " +
            $"{nameof(Value186)}: {Value186}, " +
            $"{nameof(Value187)}: {Value187}, " +
            $"{nameof(Value188)}: {Value188}, " +
            $"{nameof(Value189)}: {Value189}, " +
            $"{nameof(Value190)}: {Value190}, " +
            $"{nameof(Value191)}: {Value191}, " +
            $"{nameof(Value192)}: {Value192}, " +
            $"{nameof(Value193)}: {Value193}, " +
            $"{nameof(Value194)}: {Value194}, " +
            $"{nameof(Value195)}: {Value195}, " +
            $"{nameof(Value196)}: {Value196}, " +
            $"{nameof(Value197)}: {Value197}, " +
            $"{nameof(Value198)}: {Value198}, " +
            $"{nameof(Value199)}: {Value199}, " +
            $"{nameof(Value200)}: {Value200}, " +
            $"{nameof(Value201)}: {Value201}, " +
            $"{nameof(Value202)}: {Value202}, " +
            $"{nameof(Value203)}: {Value203}, " +
            $"{nameof(Value204)}: {Value204}, " +
            $"{nameof(Value205)}: {Value205}, " +
            $"{nameof(Value206)}: {Value206}, " +
            $"{nameof(Value207)}: {Value207}, " +
            $"{nameof(Value208)}: {Value208}, " +
            $"{nameof(Value209)}: {Value209}, " +
            $"{nameof(Value210)}: {Value210}, " +
            $"{nameof(Value211)}: {Value211}, " +
            $"{nameof(Value212)}: {Value212}, " +
            $"{nameof(Value213)}: {Value213}, " +
            $"{nameof(Value214)}: {Value214}, " +
            $"{nameof(Value215)}: {Value215}, " +
            $"{nameof(Value216)}: {Value216}, " +
            $"{nameof(Value217)}: {Value217}, " +
            $"{nameof(Value218)}: {Value218}, " +
            $"{nameof(Value219)}: {Value219}, " +
            $"{nameof(Value220)}: {Value220}, " +
            $"{nameof(Value221)}: {Value221}, " +
            $"{nameof(Value222)}: {Value222}, " +
            $"{nameof(Value223)}: {Value223}, " +
            $"{nameof(Value224)}: {Value224}, " +
            $"{nameof(Value225)}: {Value225}, " +
            $"{nameof(Value226)}: {Value226}, " +
            $"{nameof(Value227)}: {Value227}, " +
            $"{nameof(Value228)}: {Value228}, " +
            $"{nameof(Value229)}: {Value229}, " +
            $"{nameof(Value230)}: {Value230}, " +
            $"{nameof(Value231)}: {Value231}, " +
            $"{nameof(Value232)}: {Value232}, " +
            $"{nameof(Value233)}: {Value233}, " +
            $"{nameof(Value234)}: {Value234}, " +
            $"{nameof(Value235)}: {Value235}, " +
            $"{nameof(Value236)}: {Value236}, " +
            $"{nameof(Value237)}: {Value237}, " +
            $"{nameof(Value238)}: {Value238}, " +
            $"{nameof(Value239)}: {Value239}, " +
            $"{nameof(Value240)}: {Value240}, " +
            $"{nameof(Value241)}: {Value241}, " +
            $"{nameof(Value242)}: {Value242}, " +
            $"{nameof(Value243)}: {Value243}, " +
            $"{nameof(Value244)}: {Value244}, " +
            $"{nameof(Value245)}: {Value245}, " +
            $"{nameof(Value246)}: {Value246}, " +
            $"{nameof(Value247)}: {Value247}, " +
            $"{nameof(Value248)}: {Value248}, " +
            $"{nameof(Value249)}: {Value249}, " +
            $"{nameof(Value250)}: {Value250}, " +
            $"{nameof(Value251)}: {Value251}, " +
            $"{nameof(Value252)}: {Value252}, " +
            $"{nameof(Value253)}: {Value253}, " +
            $"{nameof(Value254)}: {Value254}, " +
            $"{nameof(Value255)}: {Value255}, " +
            $"{nameof(Value256)}: {Value256}";
    }

    #region IEnumerable<T>

    public IEnumerator<T> GetEnumerator() {
        // Hardly anyone will use this, so it's fine
        yield return Value1;
        yield return Value2;
        yield return Value3;
        yield return Value4;
        yield return Value5;
        yield return Value6;
        yield return Value7;
        yield return Value8;
        yield return Value9;
        yield return Value10;
        yield return Value11;
        yield return Value12;
        yield return Value13;
        yield return Value14;
        yield return Value15;
        yield return Value16;
        yield return Value17;
        yield return Value18;
        yield return Value19;
        yield return Value20;
        yield return Value21;
        yield return Value22;
        yield return Value23;
        yield return Value24;
        yield return Value25;
        yield return Value26;
        yield return Value27;
        yield return Value28;
        yield return Value29;
        yield return Value30;
        yield return Value31;
        yield return Value32;
        yield return Value33;
        yield return Value34;
        yield return Value35;
        yield return Value36;
        yield return Value37;
        yield return Value38;
        yield return Value39;
        yield return Value40;
        yield return Value41;
        yield return Value42;
        yield return Value43;
        yield return Value44;
        yield return Value45;
        yield return Value46;
        yield return Value47;
        yield return Value48;
        yield return Value49;
        yield return Value50;
        yield return Value51;
        yield return Value52;
        yield return Value53;
        yield return Value54;
        yield return Value55;
        yield return Value56;
        yield return Value57;
        yield return Value58;
        yield return Value59;
        yield return Value60;
        yield return Value61;
        yield return Value62;
        yield return Value63;
        yield return Value64;
        yield return Value65;
        yield return Value66;
        yield return Value67;
        yield return Value68;
        yield return Value69;
        yield return Value70;
        yield return Value71;
        yield return Value72;
        yield return Value73;
        yield return Value74;
        yield return Value75;
        yield return Value76;
        yield return Value77;
        yield return Value78;
        yield return Value79;
        yield return Value80;
        yield return Value81;
        yield return Value82;
        yield return Value83;
        yield return Value84;
        yield return Value85;
        yield return Value86;
        yield return Value87;
        yield return Value88;
        yield return Value89;
        yield return Value90;
        yield return Value91;
        yield return Value92;
        yield return Value93;
        yield return Value94;
        yield return Value95;
        yield return Value96;
        yield return Value97;
        yield return Value98;
        yield return Value99;
        yield return Value100;
        yield return Value101;
        yield return Value102;
        yield return Value103;
        yield return Value104;
        yield return Value105;
        yield return Value106;
        yield return Value107;
        yield return Value108;
        yield return Value109;
        yield return Value110;
        yield return Value111;
        yield return Value112;
        yield return Value113;
        yield return Value114;
        yield return Value115;
        yield return Value116;
        yield return Value117;
        yield return Value118;
        yield return Value119;
        yield return Value120;
        yield return Value121;
        yield return Value122;
        yield return Value123;
        yield return Value124;
        yield return Value125;
        yield return Value126;
        yield return Value127;
        yield return Value128;
        yield return Value129;
        yield return Value130;
        yield return Value131;
        yield return Value132;
        yield return Value133;
        yield return Value134;
        yield return Value135;
        yield return Value136;
        yield return Value137;
        yield return Value138;
        yield return Value139;
        yield return Value140;
        yield return Value141;
        yield return Value142;
        yield return Value143;
        yield return Value144;
        yield return Value145;
        yield return Value146;
        yield return Value147;
        yield return Value148;
        yield return Value149;
        yield return Value150;
        yield return Value151;
        yield return Value152;
        yield return Value153;
        yield return Value154;
        yield return Value155;
        yield return Value156;
        yield return Value157;
        yield return Value158;
        yield return Value159;
        yield return Value160;
        yield return Value161;
        yield return Value162;
        yield return Value163;
        yield return Value164;
        yield return Value165;
        yield return Value166;
        yield return Value167;
        yield return Value168;
        yield return Value169;
        yield return Value170;
        yield return Value171;
        yield return Value172;
        yield return Value173;
        yield return Value174;
        yield return Value175;
        yield return Value176;
        yield return Value177;
        yield return Value178;
        yield return Value179;
        yield return Value180;
        yield return Value181;
        yield return Value182;
        yield return Value183;
        yield return Value184;
        yield return Value185;
        yield return Value186;
        yield return Value187;
        yield return Value188;
        yield return Value189;
        yield return Value190;
        yield return Value191;
        yield return Value192;
        yield return Value193;
        yield return Value194;
        yield return Value195;
        yield return Value196;
        yield return Value197;
        yield return Value198;
        yield return Value199;
        yield return Value200;
        yield return Value201;
        yield return Value202;
        yield return Value203;
        yield return Value204;
        yield return Value205;
        yield return Value206;
        yield return Value207;
        yield return Value208;
        yield return Value209;
        yield return Value210;
        yield return Value211;
        yield return Value212;
        yield return Value213;
        yield return Value214;
        yield return Value215;
        yield return Value216;
        yield return Value217;
        yield return Value218;
        yield return Value219;
        yield return Value220;
        yield return Value221;
        yield return Value222;
        yield return Value223;
        yield return Value224;
        yield return Value225;
        yield return Value226;
        yield return Value227;
        yield return Value228;
        yield return Value229;
        yield return Value230;
        yield return Value231;
        yield return Value232;
        yield return Value233;
        yield return Value234;
        yield return Value235;
        yield return Value236;
        yield return Value237;
        yield return Value238;
        yield return Value239;
        yield return Value240;
        yield return Value241;
        yield return Value242;
        yield return Value243;
        yield return Value244;
        yield return Value245;
        yield return Value246;
        yield return Value247;
        yield return Value248;
        yield return Value249;
        yield return Value250;
        yield return Value251;
        yield return Value252;
        yield return Value253;
        yield return Value254;
        yield return Value255;
        yield return Value256;
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }

    #endregion

    #region IList<T>

    public int IndexOf(T item) {
        return AsSpan().IndexOf(item);
    }

    public void Insert(int index, T item) {
        throw new NotSupportedException();
    }

    public void RemoveAt(int index) {
        throw new NotSupportedException();
    }
    
    public void Add(T item) {
        throw new NotSupportedException();
    }

    public void Clear() {
        throw new NotSupportedException();
    }

    public bool Contains(T item) {
        throw new NotSupportedException();
    }

    public void CopyTo(T[] array, int arrayIndex) {
        throw new NotSupportedException();
    }

    public bool Remove(T item) {
        throw new NotSupportedException();
    }

    public int Count { get; } = 256;

    public bool IsReadOnly { get; } = false;

    #endregion

    public static implicit operator Span<T>(ImFixedArray256<T> array) => array.AsSpan();
    
    public static bool operator ==(ImFixedArray256<T> left, ImFixedArray256<T> right) {
        return left.Equals(right);
    }

    public static bool operator !=(ImFixedArray256<T> left, ImFixedArray256<T> right) {
        return !left.Equals(right);
    }
}
