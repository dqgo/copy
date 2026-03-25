plugins {
  id("com.android.application")
  id("org.jetbrains.kotlin.android")
  id("org.jetbrains.kotlin.plugin.compose")
}

fun envOrProp(name: String): String? {
  val prop = project.findProperty(name)?.toString()?.trim()
  if (!prop.isNullOrEmpty()) {
    return prop
  }
  val env = System.getenv(name)?.trim()
  if (!env.isNullOrEmpty()) {
    return env
  }
  return null
}

val releaseStoreFile = envOrProp("ANDROID_RELEASE_STORE_FILE")
val releaseStorePassword = envOrProp("ANDROID_RELEASE_STORE_PASSWORD")
val releaseKeyAlias = envOrProp("ANDROID_RELEASE_KEY_ALIAS")
val releaseKeyPassword = envOrProp("ANDROID_RELEASE_KEY_PASSWORD")
val hasReleaseSigning =
  !releaseStoreFile.isNullOrEmpty() &&
  !releaseStorePassword.isNullOrEmpty() &&
  !releaseKeyAlias.isNullOrEmpty() &&
  !releaseKeyPassword.isNullOrEmpty()

android {
  namespace = "com.clipboardsync"
  compileSdk = 35

  buildFeatures {
    compose = true
  }

  if (hasReleaseSigning) {
    signingConfigs {
      create("release") {
        storeFile = file(releaseStoreFile!!)
        storePassword = releaseStorePassword
        keyAlias = releaseKeyAlias
        keyPassword = releaseKeyPassword
      }
    }
  }

  defaultConfig {
    applicationId = "com.clipboardsync"
    minSdk = 26
    targetSdk = 35
    versionCode = 1
    versionName = "0.1.0"
  }

  buildTypes {
    release {
      isMinifyEnabled = false
      if (hasReleaseSigning) {
        signingConfig = signingConfigs.getByName("release")
      }
      proguardFiles(
        getDefaultProguardFile("proguard-android-optimize.txt"),
        "proguard-rules.pro"
      )
    }
  }

  compileOptions {
    sourceCompatibility = JavaVersion.VERSION_17
    targetCompatibility = JavaVersion.VERSION_17
  }

  kotlinOptions {
    jvmTarget = "17"
  }
}

dependencies {
  implementation("androidx.core:core-ktx:1.15.0")
  implementation("androidx.activity:activity-compose:1.10.1")
  implementation(platform("androidx.compose:compose-bom:2025.02.00"))
  implementation("androidx.compose.ui:ui")
  implementation("androidx.compose.ui:ui-tooling-preview")
  implementation("androidx.compose.material3:material3")
  debugImplementation("androidx.compose.ui:ui-tooling")
}
