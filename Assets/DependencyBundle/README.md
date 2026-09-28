# Package Structure dependency installer

Import the unitypackage into a Unity 6000.3 project, then open **Tools > Package Structure > Dependency Installer**. Install Unity MCP, UniTask, VContainer, and DOTween with their individual buttons. Each button fetches a pinned version from the official upstream source. UniTask and VContainer must be installed before enabling the Event Bus button.

The Event Bus is project code, not a third-party online package. Its button copies the included source templates into `Assets/Scripts/EventBus` and creates an assembly definition referencing UniTask and VContainer.

After DOTween imports, open **Tools > Demigiant > DOTween Utility Panel** and click **Setup DOTween...** as required by Demigiant.

This installer contains no local UPM tarballs. The online Git packages and DOTween download require network access. Unity MCP's external Python/uv server runtime must be configured separately in its setup window.
