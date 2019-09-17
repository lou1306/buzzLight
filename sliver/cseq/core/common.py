funcPrefixChange = {}
funcPrefixChange['__VERIFIER_atomic'] = '__CS_atomic'

changeID = {}

changeID['PTHREAD_MUTEX_INITIALIZER'] = '__cs_MUTEX_INITIALIZER'
changeID['PTHREAD_COND_INITIALIZER'] = '__cs_COND_INITIALIZER'
changeID['PTHREAD_RWLOCK_INITIALIZER'] = '__cs_RWLOCK_INITIALIZER'
changeID['PTHREAD_BARRIER_SERIAL_THREAD'] = '__cs_BARRIER_SERIAL_THREAD'
changeID['PTHREAD_CANCEL_ASYNCHRONOUS'] = '__cs_CANCEL_ASYNCHRONOUS'
changeID['PTHREAD_CANCEL_ENABLE'] = '__cs_CANCEL_ENABLE'
changeID['PTHREAD_CANCEL_DEFERRED'] = '__cs_CANCEL_DEFERRED'
changeID['PTHREAD_CANCEL_DISABLE'] = '__cs_CANCEL_DISABLE'
changeID['PTHREAD_CANCELED'] = '__cs_CANCELED'
changeID['PTHREAD_CREATE_DETACHED'] = '__cs_CREATE_DETACHED'
changeID['PTHREAD_CREATE_JOINABLE'] = '__cs_CREATE_JOINABLE'
changeID['PTHREAD_EXPLICIT_SCHED'] = '__cs_EXPLICIT_SCHED'
changeID['PTHREAD_INHERIT_SCHED'] = '__cs_INHERIT_SCHED'
changeID['PTHREAD_MUTEX_DEFAULT'] = '__cs_MUTEX_DEFAULT'
changeID['PTHREAD_MUTEX_ERRORCHECK'] = '__cs_MUTEX_ERRORCHECK'
changeID['PTHREAD_MUTEX_NORMAL'] = '__cs_MUTEX_NORMAL'
changeID['PTHREAD_MUTEX_RECURSIVE'] = '__cs_MUTEX_RECURSIVE'
changeID['PTHREAD_MUTEX_ROBUST'] = '__cs_MUTEX_ROBUST'
changeID['PTHREAD_MUTEX_STALLED'] = '__cs_MUTEX_STALLED'
changeID['PTHREAD_ONCE_INIT'] = '__cs_ONCE_INIT'
changeID['PTHREAD_PRIO_INHERIT'] = '__cs_PRIO_INHERIT'
changeID['PTHREAD_PRIO_NONE'] = '__cs_PRIO_NONE'
changeID['PTHREAD_PRIO_PROTECT'] = '__cs_PRIO_PROTECT'
changeID['PTHREAD_PROCESS_SHARED'] = '__cs_PROCESS_SHARED'
changeID['PTHREAD_PROCESS_PRIVATE'] = '__cs_PROCESS_PRIVATE'
changeID['PTHREAD_SCOPE_PROCESS'] = '__cs_SCOPE_PROCESS'
changeID['PTHREAD_SCOPE_SYSTEM'] = '__cs_SCOPE_SYSTEM'

changeID['pthread_attr_t'] = '__cs_attr_t'
changeID['pthread_cond_t'] = '__cs_cond_t'
changeID['pthread_condattr_t'] = '__cs_condattr_t'
changeID['pthread_key_t'] = '__cs_key_t'
changeID['pthread_mutex_t'] = '__cs_mutex_t'
changeID['pthread_mutexattr_t'] = '__cs_mutexattr_t'
changeID['pthread_once_t'] = '__cs_once_t'
changeID['pthread_rwlock_t'] = '__cs_rwlock_t'
changeID['pthread_rwlockattr_t'] = '__cs_rwlockattr_t'
changeID['pthread_t'] = '__cs_t'

changeID['pthread_attr_destroy'] = '__cs_attr_destroy'
changeID['pthread_attr_getdetachstate'] = '__cs_attr_getdetachstate'
changeID['pthread_attr_getguardsize'] = '__cs_attr_getguardsize'
changeID['pthread_attr_getinheritsched'] = '__cs_attr_getinheritsched'
changeID['pthread_attr_getschedparam'] = '__cs_attr_getschedparam'
changeID['pthread_attr_getschedpolicy'] = '__cs_attr_getschedpolicy'
changeID['pthread_attr_getscope'] = '__cs_attr_getscope'
changeID['pthread_attr_getstackaddr'] = '__cs_attr_getstackaddr'
changeID['pthread_attr_getstacksize'] = '__cs_attr_getstacksize'
changeID['pthread_attr_init'] = '__cs_attr_init'
changeID['pthread_attr_setdetachstate'] = '__cs_attr_setdetachstate'
changeID['pthread_attr_setguardsize'] = '__cs_attr_setguardsize'
changeID['pthread_attr_setinheritsched'] = '__cs_attr_setinheritsched'
changeID['pthread_attr_setschedparam'] = '__cs_attr_setschedparam'
changeID['pthread_attr_setschedpolicy'] = '__cs_attr_setschedpolicy'
changeID['pthread_attr_setscope'] = '__cs_attr_setscope'
changeID['pthread_attr_setstackaddr'] = '__cs_attr_setstackaddr'
changeID['pthread_attr_setstacksize'] = '__cs_attr_setstacksize'
changeID['pthread_cancel'] = '__cs_cancel'
changeID['pthread_cleanup_push'] = '__cs_cleanup_push'
changeID['pthread_cleanup_pop'] = '__cs_cleanup_pop'
changeID['pthread_cond_broadcast'] = '__cs_cond_broadcast'
changeID['pthread_cond_destroy'] = '__cs_cond_destroy'
changeID['pthread_cond_init'] = '__cs_cond_init'
changeID['pthread_cond_signal'] = '__cs_cond_signal'
changeID['pthread_cond_timedwait'] = '__cs_cond_timedwait'
changeID['pthread_cond_wait'] = '__cs_cond_wait'
changeID['pthread_condattr_destroy'] = '__cs_condattr_destroy'
changeID['pthread_condattr_getpshared'] = '__cs_condattr_getpshared'
changeID['pthread_condattr_init'] = '__cs_condattr_init'
changeID['pthread_condattr_setpshared'] = '__cs_condattr_setpshared'
changeID['pthread_create'] = '__cs_create'
changeID['pthread_detach'] = '__cs_detach'
changeID['pthread_equal'] = '__cs_equal'
changeID['pthread_exit'] = '__cs_exit'
changeID['pthread_getconcurrency'] = '__cs_getconcurrency'
changeID['pthread_getschedparam'] = '__cs_getschedparam'
changeID['pthread_getspecific'] = '__cs_getspecific'
changeID['pthread_join'] = '__cs_join'
changeID['pthread_key_create'] = '__cs_key_create'
changeID['pthread_key_delete'] = '__cs_key_delete'
changeID['pthread_mutex_destroy'] = '__cs_mutex_destroy'
changeID['pthread_mutex_getprioceiling'] = '__cs_mutex_getprioceiling'
changeID['pthread_mutex_init'] = '__cs_mutex_init'
changeID['pthread_mutex_lock'] = '__cs_mutex_lock'
changeID['pthread_mutex_setprioceiling'] = '__cs_mutex_setprioceiling'
changeID['pthread_mutex_trylock'] = '__cs_mutex_trylock'
changeID['pthread_mutex_unlock'] = '__cs_mutex_unlock'
changeID['pthread_mutexattr_destroy'] = '__cs_mutexattr_destroy'
changeID['pthread_mutexattr_getprioceiling'] = '__cs_mutexattr_getprioceiling'
changeID['pthread_mutexattr_getprotocol'] = '__cs_mutexattr_getprotocol'
changeID['pthread_mutexattr_getpshared'] = '__cs_mutexattr_getpshared'
changeID['pthread_mutexattr_gettype'] = '__cs_mutexattr_gettype'
changeID['pthread_mutexattr_init'] = '__cs_mutexattr_init'
changeID['pthread_mutexattr_setprioceiling'] = '__cs_mutexattr_setprioceiling'
changeID['pthread_mutexattr_setprotocol'] = '__cs_mutexattr_setprotocol'
changeID['pthread_mutexattr_setpshared'] = '__cs_mutexattr_setpshared'
changeID['pthread_mutexattr_settype'] = '__cs_mutexattr_settype'
changeID['pthread_once'] = '__cs_once'
changeID['pthread_rwlock_destroy'] = '__cs_rwlock_destroy'
changeID['pthread_rwlock_init'] = '__cs_rwlock_init'
changeID['pthread_rwlock_rdlock'] = '__cs_rwlock_rdlock'
changeID['pthread_rwlock_tryrdlock'] = '__cs_rwlock_tryrdlock'
changeID['pthread_rwlock_trywrlock'] = '__cs_rwlock_trywrlock'
changeID['pthread_rwlock_unlock'] = '__cs_rwlock_unlock'
changeID['pthread_rwlock_wrlock'] = '__cs_rwlock_wrlock'
changeID['pthread_rwlockattr_destroy'] = '__cs_rwlockattr_destroy'
changeID['pthread_rwlockattr_getpshared'] = '__cs_rwlockattr_getpshared'
changeID['pthread_rwlockattr_init'] = '__cs_rwlockattr_init'
changeID['pthread_rwlockattr_setpshared'] = '__cs_rwlockattr_setpshared'
changeID['pthread_self'] = '__cs_self'
changeID['pthread_setcancelstate'] = '__cs_setcancelstate'
changeID['pthread_setcanceltype'] = '__cs_setcanceltype'
changeID['pthread_setconcurrency'] = '__cs_setconcurrency'
changeID['pthread_setschedparam'] = '__cs_setschedparam'
changeID['pthread_setspecific'] = '__cs_setspecific'
changeID['pthread_testcancel'] = '__cs_testcancel'

changeID['pthread_barrier_t'] = '__cs_barrier_t'
changeID['pthread_barrierattr_t'] = '__cs_barrierattr_t'
changeID['pthread_barrier_destroy'] = '__cs_barrier_destroy'
changeID['pthread_barrier_init'] = '__cs_barrier_init'
changeID['pthread_barrier_wait'] = '__cs_barrier_wait'
changeID['pthread_barrierattr_destroy'] = '__cs_barrierattr_destroy'
changeID['pthread_barrierattr_getpshared'] = '__cs_barrierattr_getpshared'
changeID['pthread_barrierattr_init'] = '__cs_barrierattr_init'
changeID['pthread_barrierattr_setpshared'] = '__cs_barrierattr_setpshared'

changeID['pthread_sigmask'] = '__cs_sigmask'

changeID['malloc'] = '__cs_safe_malloc'   # or change to __cs_unsafe_malloc if you want to consider when malloc()s fails!

