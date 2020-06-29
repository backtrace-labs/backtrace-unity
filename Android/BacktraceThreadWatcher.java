package backtrace.io.backtrace_unity_android_plugin;

/**
 * This class is a representation of the state of the thread,
 * the user's thread has access to one counter and BacktraceWatchdog to the other.
 * By comparing the values of the counters it is possible to
 * determine whether the thread has been hanged.
 */
public class BacktraceThreadWatcher {
    private int counter;
    private int privateCounter;
    private int timeout;
    private int delay;
    private long lastTimestamp;
    private boolean active;

    /**
     * Thread watcher which is using to monitoring thread state
     *
     * @param timeout maximum time in milliseconds after which should check if the main thread is not hanged
     * @param delay   delay in milliseconds from which we should monitor the thread
     */
    BacktraceThreadWatcher(int timeout, int delay) {
        this.timeout = timeout;
        this.delay = delay;
        setActive(true);
    }

    /**
     * @return timeout value in milliseconds
     */
    int getTimeout() {
        return timeout;
    }

    /**
     * @return delay value in milliseconds
     */
    int getDelay() {
        return delay;
    }

    /**
     * @return last timestamp when the thread was checked
     */
    long getLastTimestamp() {
        return lastTimestamp;
    }

    /**
     * @param lastTimestamp new value of last timestamp when thread was checked
     */
    void setLastTimestamp(long lastTimestamp) {
        this.lastTimestamp = lastTimestamp;
    }

    /**
     * Check is watcher for thread is active
     *
     * @return is thread watcher active
     */
    synchronized boolean isActive() {
        return active;
    }

    /**
     * Set status of thread watcher
     *
     * @param active if active value is false thread is not observed
     */
    synchronized void setActive(boolean active) {
        this.active = active;
    }

    /**
     * Increase thread private counter by 1
     */
    void tickPrivateCounter() {
        privateCounter++;
    }

    /**
     * @return thread private counter
     */
    int getPrivateCounter() {
        return privateCounter;
    }

    /**
     * @param privateCounter new value of private counter
     */
    void setPrivateCounter(int privateCounter) {
        this.privateCounter = privateCounter;
    }

    /**
     * @return thread counter
     */
    synchronized int getCounter() {
        return counter;
    }

    /**
     * Increase thread counter by 1
     */
    public synchronized void tickCounter() {
        counter++;
    }
}