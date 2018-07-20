在线生成 android 编译环境安装

0. 下载并安装jdk
http://www.oracle.com/technetwork/java/javase/downloads/jdk8-downloads-2133151.html

1. 下载 sdk tools，解压
下载路径
https://developer.android.com/studio/#downloads

2. sdk\tools\bin>sdkmanager， 执行以下命令，安装编译环境
sdkmanager build-tools;27.0.3
sdkmanager platforms;android-27

3. 下载gradle，并添加到环境变量
https://gradle.org/releases/


4. 部署站点
AppBuilder 需要创建指定用户，参考：https://segmentfault.com/a/1190000004938055

5. 复制 安卓项目源码 到站点目录
源码包含 app目录 和 build.gradle, gradle.properties, local.properties, settings.gradle


6. 源代码目录，执行
gradle assembleRelease