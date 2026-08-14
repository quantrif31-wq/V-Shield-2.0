-keepattributes *Annotation*
-keep class com.vshield.mobile.data.model.** { *; }
-keepclassmembers class com.vshield.mobile.data.model.** { *; }

-dontwarn com.google.errorprone.annotations.**

-keepclassmembers class * {
    @com.google.gson.annotations.SerializedName <fields>;
}
