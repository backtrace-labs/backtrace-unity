package backtrace.io.backtrace_unity_android_plugin;

import android.content.Context;
import android.os.Build;
import android.os.PowerManager;
import android.util.Log;

import java.io.BufferedReader;
import java.io.File;
import java.io.FileReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.util.HashMap;
import java.util.Locale;
import java.util.Map;

public class BacktraceAttributes {

    private Context context;
    private final static transient String LOG_TAG = BacktraceAttributes.class.getSimpleName();
    private static Map<String, String> _attributeMapping = new HashMap<String, String>();
    static {
        _attributeMapping.put("FDSize", "descriptor.count");
        _attributeMapping.put("VmPeak", "vm.vma.peak");
        _attributeMapping.put("VmSize", "vm.vma.size");
        _attributeMapping.put("VmLck", "vm.locked.size");
        _attributeMapping.put("VmHWM", "vm.rss.peak");
        _attributeMapping.put("VmRSS", "vm.rss.size");
        _attributeMapping.put("VmStk", "vm.stack.size");
        _attributeMapping.put("VmData", "vm.data");
        _attributeMapping.put("VmExe", "vm.exe");
        _attributeMapping.put("VmLib", "vm.shared.size");
        _attributeMapping.put("VmPTE", "vm.pte.size");
        _attributeMapping.put("VmSwap", "vm.swap.size");

        _attributeMapping.put("State", "state");


        _attributeMapping.put("voluntary_ctxt_switches", "sched.cs.voluntary");
        _attributeMapping.put("nonvoluntary_ctxt_switches", "sched.cs.involuntary");

        _attributeMapping.put("SigPnd", "vm.sigpnd");
        _attributeMapping.put("ShdPnd", "vm.shdpnd");
        _attributeMapping.put("Threads", "vm.threads");

        _attributeMapping.put("MemTotal", "system.memory.total");
        _attributeMapping.put("MemFree", "system.memory.free");
        _attributeMapping.put("Buffers", "system.memory.buffers");
        _attributeMapping.put("Cached", "system.memory.cached");
        _attributeMapping.put("SwapCached", "system.memory.swap.cached");
        _attributeMapping.put("Active", "system.memory.active");
        _attributeMapping.put("Inactive", "system.memory.inactive");
        _attributeMapping.put("SwapTotal", "system.memory.swap.total");
        _attributeMapping.put("SwapFree", "system.memory.swap.free");
        _attributeMapping.put("Dirty", "system.memory.dirty");
        _attributeMapping.put("Writeback", "system.memory.writeback");
        _attributeMapping.put("Slab", "system.memory.slab");
        _attributeMapping.put("VmallocTotal", "system.memory.vmalloc.total");
        _attributeMapping.put("VmallocUsed", "system.memory.vmalloc.used");
        _attributeMapping.put("VmallocChunk", "system.memory.vmalloc.chunk");
    }

    public Map<String, String> GetAttributes(Context unityContext) {

        context = unityContext;
        HashMap<String, String> result = getProcessAttributes();
        result.put("app.storage_used", getAppUsedStorageSize());
        result.put("device.cpu.temperature", getCpuTemperature());
        result.put("device.is_power_saving_mode", Boolean.toString(isPowerSavingMode()));
        result.put("culture", Locale.getDefault().getDisplayLanguage());
        result.put("device.sdk", Integer.toString(Build.VERSION.SDK_INT));
        result.put("device.manufacturer", Build.MANUFACTURER);

        result.entrySet().iterator();
        return result;
    }


    private boolean isPowerSavingMode() {
        if (Build.VERSION.SDK_INT < 21) {
            return false;
        }
        PowerManager powerManager = (PowerManager) this.context.getSystemService(Context
                .POWER_SERVICE);
        return powerManager.isPowerSaveMode();
    }

    public HashMap<String, String> getProcessAttributes() {

        HashMap<String, String> result = new HashMap<>();

        int processId = android.os.Process.myPid();
        if (processId < 0) {
            Log.d(LOG_TAG, "Failed to read process id");
            return result;
        }
        String processAttributes = String.format("/proc/%d/status", processId);
        String memoryAttributes = "/proc/meminfo";
        return  readAttributesFromFile(
                memoryAttributes,
                readAttributesFromFile(processAttributes, result));
    }

    private HashMap<String,String> readAttributesFromFile(String path, HashMap<String,String> attributes) {
        File file = new File(path);

        StringBuilder text = new StringBuilder();
        try {
            BufferedReader br = new BufferedReader(new FileReader(file));
            String line;

            while ((line = br.readLine()) != null) {
                String[] entry = line.split(":", 2);
                String key = entry[0].trim();
                if(!_attributeMapping.containsKey(key)){
                   continue;
                }
                key = _attributeMapping.get(key);
                String value = entry[1].trim();
                if(value.endsWith("kB")){
                    value = value.substring(0,value.lastIndexOf('k')).trim();
                }
                attributes.put(key, value);
            }
            br.close();
        } catch (IOException e) {
            Log.d(LOG_TAG, "Cannot read process information. Reason:" + e.getMessage());
            attributes.put("parseError", e.getMessage());
        }

        return attributes;
    }


    public String getCpuTemperature() {
        Process p;
        try {
            p = Runtime.getRuntime().exec("cat sys/class/thermal/thermal_zone0/temp");
            p.waitFor();
            BufferedReader reader = new BufferedReader(new InputStreamReader(p.getInputStream()));

            String line = reader.readLine();
            if (line == null) {
                return "0.0";
            }
            return Float.toString(Float.parseFloat(line) / 1000.0f);
        } catch (Exception e) {
            return "0.0";
        }
    }

    public String getAppUsedStorageSize() {
        long freeSize = 0L;
        long totalSize = 0L;
        long usedSize = -1L;
        try {
            Runtime info = Runtime.getRuntime();
            freeSize = info.freeMemory();
            totalSize = info.totalMemory();
            usedSize = totalSize - freeSize;
        } catch (Exception e) {
            e.printStackTrace();
        }
        return Long.toString(usedSize);
    }
}
